Imports AssetsTools.NET
Imports AssetsTools.NET.Extra
Imports System.IO
Imports System.IO.Compression
Imports System.Diagnostics

Public Class AssetProcessor
    Public Property Loc As Localization
    Public Event LogMessage(message As String)
    Public Event ProgressUpdate(value As Integer, max As Integer)

    Private ReadOnly ASSET_EXTENSIONS As String() = {".assets", ".bundle", ".unity3d", ".sharedassets", ".ress", ".dat"}

    Public Class ShaderInfo
        Public Property Name As String
        Public Property PathId As Long
        Public Property FilePath As String
        Public Property OriginalFilePath As String
    End Class

    Private Class RawPatchRecord
        Public Property ShaderName As String
        Public Property PathId As Long
        Public Property AbsoluteOffset As Long
        Public Property ByteSize As Integer
        Public Property ModifiedBytes As Byte()
        Public Property ChangedValueCount As Integer
    End Class

    Private Sub Log(msg As String)
        RaiseEvent LogMessage(msg)
    End Sub

    Private Function IsKnownAssetFile(filePath As String) As Boolean
        Return ASSET_EXTENSIONS.Contains(Path.GetExtension(filePath).ToLowerInvariant())
    End Function

    Private Function IsBackupFile(filePath As String) As Boolean
        Return filePath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsApkFile(filePath As String) As Boolean
        Return String.Equals(Path.GetExtension(filePath), ".apk", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function IsScanAllCandidate(filePath As String) As Boolean
        If IsBackupFile(filePath) Then Return False
        If IsApkFile(filePath) Then Return False
        Return True
    End Function

    Public Function ScanShaders(targetPath As String, scanAll As Boolean, ByRef outApkMap As Dictionary(Of String, String)) As List(Of ShaderInfo)
        Dim foundShaders As New List(Of ShaderInfo)()
        Dim assetFiles As New List(Of String)()
        Dim apkFiles As New List(Of String)()

        If Directory.Exists(targetPath) Then
            Dim allFiles = Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories)
            For Each f In allFiles
                If IsKnownAssetFile(f) Then
                    assetFiles.Add(f)
                ElseIf IsApkFile(f) Then
                    apkFiles.Add(f)
                End If
            Next
        ElseIf File.Exists(targetPath) Then
            If IsKnownAssetFile(targetPath) Then
                assetFiles.Add(targetPath)
            ElseIf IsApkFile(targetPath) Then
                apkFiles.Add(targetPath)
            End If
        End If

        Dim scanTargets As New List(Of String)()
        Dim apkSessionTempDir As String = Nothing

        If assetFiles.Count = 0 AndAlso apkFiles.Count > 0 Then
            Dim apkToProcess = apkFiles(0)
            Log(String.Format(Loc.GetString("LogApkDetected"), Path.GetFileName(apkToProcess)))
            apkSessionTempDir = Path.Combine(Path.GetTempPath(), "demosaic_session_" & Guid.NewGuid().ToString().Substring(0, 8))
            
            Dim apkName = Path.GetFileNameWithoutExtension(apkToProcess)
            Dim tempSubDir = Path.Combine(apkSessionTempDir, apkName)
            Directory.CreateDirectory(tempSubDir)
            
            Log(Loc.GetString("LogExtracting"))
            Try
                ZipFile.ExtractToDirectory(apkToProcess, tempSubDir)
                outApkMap(apkToProcess) = tempSubDir
            Catch ex As Exception
                Log(String.Format(Loc.GetString("LogError"), "Zip Extract Failed: " & ex.Message))
                Try
                    If Not String.IsNullOrEmpty(apkSessionTempDir) AndAlso Directory.Exists(apkSessionTempDir) Then
                        Directory.Delete(apkSessionTempDir, True)
                    End If
                Catch
                End Try
                Return foundShaders
            End Try

            Dim extractedFiles = Directory.GetFiles(tempSubDir, "*", SearchOption.AllDirectories)
            For Each f In extractedFiles
                If IsKnownAssetFile(f) Then
                    scanTargets.Add(f)
                End If
            Next
        Else
            Log(Loc.GetString("LogNormalMode"))
            If scanAll Then
                If Directory.Exists(targetPath) Then
                    scanTargets.AddRange(Directory.GetFiles(targetPath, "*", SearchOption.AllDirectories).Where(
                        Function(f) IsScanAllCandidate(f)))
                Else
                    If File.Exists(targetPath) AndAlso IsScanAllCandidate(targetPath) Then
                        scanTargets.Add(targetPath)
                    End If
                End If
            Else
                scanTargets.AddRange(assetFiles)
            End If
        End If

        Dim totalFiles = scanTargets.Count
        RaiseEvent ProgressUpdate(0, totalFiles)
        Log(String.Format(Loc.GetString("LogScanStart"), totalFiles))

        Dim am As New AssetsManager()
        If File.Exists("classdata.tpk") Then
            Try
                am.LoadClassPackage("classdata.tpk")
            Catch
            End Try
        End If

        For i As Integer = 0 To scanTargets.Count - 1
            Dim filePath = scanTargets(i)
            ScanSingleFile(filePath, foundShaders, am)

            ' Lightweight GC every file; expensive LOH compaction only periodically
            If i Mod 20 = 0 Then
                System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce
            End If
            GC.Collect(2, GCCollectionMode.Forced, False)
            GC.WaitForPendingFinalizers()

            RaiseEvent ProgressUpdate(i + 1, totalFiles)
        Next

        System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce
        GC.Collect(2, GCCollectionMode.Forced, True, True)
        GC.WaitForPendingFinalizers()
        Log(Loc.GetString("LogScanComplete"))
        Return foundShaders
    End Function

    Private Sub ScanSingleFile(filePath As String, foundShaders As List(Of ShaderInfo), am As AssetsManager)

        Try
            ' Check if bundle
            Dim isBundle = False
            Using fs As FileStream = File.OpenRead(filePath)
                If fs.Length > 20 Then
                    Dim br As New BinaryReader(fs)
                    Dim magic As String = New String(br.ReadChars(7))
                    If magic.StartsWith("UnityFS") OrElse magic.StartsWith("UnityWeb") OrElse magic.StartsWith("UnityRaw") Then
                        isBundle = True
                    End If
                End If
            End Using

            Dim assetInsts As New List(Of AssetsFileInstance)()

            Dim bunInst As BundleFileInstance = Nothing
            If isBundle Then
                bunInst = am.LoadBundleFile(filePath, False)
                For j As Integer = 0 To bunInst.file.BlockAndDirInfo.DirectoryInfos.Count - 1
                    Dim dirInfo = bunInst.file.BlockAndDirInfo.DirectoryInfos(j)
                    If dirInfo.Name.EndsWith(".resS") OrElse dirInfo.Name.EndsWith(".resource") Then Continue For
                    Try
                        Dim assetInst = am.LoadAssetsFileFromBundle(bunInst, j, False)
                        If assetInst IsNot Nothing Then assetInsts.Add(assetInst)
                    Catch ex As Exception
                    End Try
                Next
            Else
                Try
                    Dim assetInst = am.LoadAssetsFile(filePath, False)
                    assetInsts.Add(assetInst)
                Catch ex As Exception
                End Try
            End If

            For Each inst In assetInsts
                If am.ClassDatabase Is Nothing AndAlso inst.file.Metadata.TypeTreeEnabled = False AndAlso File.Exists("classdata.tpk") Then
                    Try
                        am.LoadClassDatabaseFromPackage(inst.file.Metadata.UnityVersion)
                    Catch
                    End Try
                End If

                ' We try to find Shader objects (ClassID = 48)
                For Each info In inst.file.GetAssetsOfType(AssetClassID.Shader)
                    Try
                        Dim baseField = am.GetBaseField(inst, info)
                        If baseField IsNot Nothing Then
                            Dim shaderName = baseField.Get("m_Name").AsString
                            If String.IsNullOrWhiteSpace(shaderName) Then
                                Try
                                    shaderName = baseField.Get("m_ParsedForm").Get("m_Name").AsString
                                Catch
                                    shaderName = "Unknown Shader"
                                End Try
                            End If
                            foundShaders.Add(New ShaderInfo With {
                                .Name = shaderName,
                                .PathId = info.PathId,
                                .FilePath = filePath
                            })
                        End If
                    Catch ex As Exception
                    End Try
                Next
                
                Try
                    inst.file.Close()
                Catch
                End Try
            Next
            
            If isBundle AndAlso bunInst IsNot Nothing Then
                Try
                    bunInst.file.Close()
                Catch
                End Try
            End If
        Catch ex As Exception
        End Try

        ' Unload file data only - preserve ClassPackage/ClassDatabase cache
        Try
            am.UnloadAllAssetsFiles(True)
        Catch
        End Try
        Try
            am.UnloadAllBundleFiles()
        Catch
        End Try
    End Sub

    Public Sub ProcessAssets(shadersToProcess As Dictionary(Of String, HashSet(Of Long)), backup As Boolean)
        Dim i As Integer = 0
        Dim am As New AssetsManager()
        If File.Exists("classdata.tpk") Then
            Try
                am.LoadClassPackage("classdata.tpk")
            Catch
            End Try
        End If

        For Each kvp In shadersToProcess
            Dim filePath = kvp.Key
            Dim pathIds = kvp.Value

            ProcessSingleFile(filePath, pathIds, backup, am)

            ' Force GC every file
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce
            GC.Collect(2, GCCollectionMode.Forced, True, True)
            GC.WaitForPendingFinalizers()
            i += 1
        Next
        
        System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce
        GC.Collect(2, GCCollectionMode.Forced, True, True)
        GC.WaitForPendingFinalizers()
    End Sub

    Private Sub ProcessSingleFile(filePath As String, pathIds As HashSet(Of Long), backup As Boolean, am As AssetsManager)
            Try
                Dim isBundle = False
                Using fs As FileStream = File.OpenRead(filePath)
                    If fs.Length > 20 Then
                        Dim br As New BinaryReader(fs)
                        Dim magic As String = New String(br.ReadChars(7))
                        If magic.StartsWith("UnityFS") OrElse magic.StartsWith("UnityWeb") OrElse magic.StartsWith("UnityRaw") Then
                            isBundle = True
                        End If
                    End If
                End Using

                Dim modifiedAny = False
                Dim bunInst As BundleFileInstance = Nothing
                Dim assetInsts As New Dictionary(Of Integer, AssetsFileInstance)()

                If isBundle Then
                    bunInst = am.LoadBundleFile(filePath, False)
                    For j As Integer = 0 To bunInst.file.BlockAndDirInfo.DirectoryInfos.Count - 1
                        Dim dirInfo = bunInst.file.BlockAndDirInfo.DirectoryInfos(j)
                        If dirInfo.Name.EndsWith(".resS") OrElse dirInfo.Name.EndsWith(".resource") Then Continue For
                        Try
                            Dim assetInst = am.LoadAssetsFileFromBundle(bunInst, j, False)
                            If assetInst IsNot Nothing Then assetInsts.Add(j, assetInst)
                        Catch ex As Exception
                        End Try
                    Next
                Else
                    Dim assetInst = am.LoadAssetsFile(filePath, False)
                    assetInsts.Add(0, assetInst)
                End If

                Dim rawPatchMode = Not isBundle
                Dim rawPatchRecords As New List(Of RawPatchRecord)()

                For Each kvp In assetInsts
                    Dim inst = kvp.Value
                    If am.ClassDatabase Is Nothing AndAlso inst.file.Metadata.TypeTreeEnabled = False AndAlso File.Exists("classdata.tpk") Then
                        Try
                            am.LoadClassDatabaseFromPackage(inst.file.Metadata.UnityVersion)
                        Catch
                        End Try
                    End If
                    
                    For Each info In inst.file.GetAssetsOfType(AssetClassID.Shader)
                        If Not pathIds.Contains(info.PathId) Then Continue For

                        Try
                            Dim baseField = am.GetBaseField(inst, info)
                            Dim shaderName = baseField.Get("m_Name").AsString
                            ' We keep technical logs in English intentionally
                            Log($"[+] Processing: {shaderName} in {Path.GetFileName(filePath)} (pathId:{info.PathId})")

                            Dim parsedForm = baseField.Get("m_ParsedForm")
                            If Not parsedForm.IsDummy() Then
                                Dim subShadersField = parsedForm.Get("m_SubShaders")
                                Dim subShadersArray = subShadersField.Get("Array")
                                Dim modified = False
                                Dim changedValueCount = 0

                                If Not subShadersArray.IsDummy() Then
                                    For Each subShader In subShadersArray.Children
                                        Dim passesField = subShader.Get("m_Passes")
                                        Dim passesArray = passesField.Get("Array")
                                        If Not passesArray.IsDummy() Then
                                            For Each shaderPass In passesArray.Children
                                                Dim state = shaderPass.Get("m_State")
                                                If Not state.IsDummy() Then
                                                    ' Iterate children of m_State to find colMask
                                                    For Each stateProp In state.Children
                                                        Dim colMask = stateProp.Get("colMask")
                                                        If Not colMask.IsDummy() Then
                                                            Dim valField = colMask.Get("val")
                                                            If Not valField.IsDummy() AndAlso valField.AsInt > 0 Then
                                                                valField.AsInt = 0
                                                                modified = True
                                                                changedValueCount += 1
                                                            End If
                                                        End If
                                                    Next
                                                End If
                                            Next
                                        End If
                                    Next
                                End If

                                If modified Then
                                    Log("   -> modified colMask to 0")
                                    Dim bytes = baseField.WriteToByteArray()
                                    If rawPatchMode Then
                                        rawPatchRecords.Add(New RawPatchRecord With {
                                            .ShaderName = shaderName,
                                            .PathId = info.PathId,
                                            .AbsoluteOffset = inst.file.Header.DataOffset + info.ByteOffset,
                                            .ByteSize = CInt(info.ByteSize),
                                            .ModifiedBytes = bytes,
                                            .ChangedValueCount = changedValueCount
                                        })
                                    Else
                                        info.Replacer = New ContentReplacerFromBuffer(bytes)
                                    End If
                                    modifiedAny = True
                                End If
                            Else
                                Log("    -> Skipping: m_ParsedForm not found.")
                            End If
                        Catch ex As Exception
                            Log("    -> Error processing shader: " & ex.Message)
                        End Try
                    Next
                Next

                If modifiedAny Then
                    If rawPatchMode Then
                        For Each kvp In assetInsts
                            Try
                                kvp.Value.file.Close()
                            Catch
                            End Try
                        Next
                        If isBundle AndAlso bunInst IsNot Nothing Then
                            Try
                                bunInst.file.Close()
                            Catch
                            End Try
                        End If
                        assetInsts.Clear()
                        bunInst = Nothing
                        Try
                            am.UnloadAllAssetsFiles(True)
                        Catch
                        End Try
                        Try
                            am.UnloadAllBundleFiles()
                        Catch
                        End Try
                        SaveRawPatchedFile(filePath, rawPatchRecords, backup)
                        Return
                    ElseIf RequiresRawPatchWriter(assetInsts) Then
                        Log(String.Format(Loc.GetString("LogError"), "Unsafe save blocked for Unity 6000 bundle. Raw patching supports loose .assets files only: " & Path.GetFileName(filePath)))
                        For Each kvp In assetInsts
                            Try
                                kvp.Value.file.Close()
                            Catch
                            End Try
                        Next
                        If isBundle AndAlso bunInst IsNot Nothing Then
                            Try
                                bunInst.file.Close()
                            Catch
                            End Try
                        End If
                        assetInsts.Clear()
                        bunInst = Nothing
                        Try
                            am.UnloadAllAssetsFiles(True)
                        Catch
                        End Try
                        Try
                            am.UnloadAllBundleFiles()
                        Catch
                        End Try
                        Return
                    End If

                    If backup Then
                        Dim backupPath = filePath & ".bak"
                        Try
                            If Not File.Exists(backupPath) Then
                                Log(String.Format(Loc.GetString("LogBackup"), Path.GetFileName(backupPath)))
                                File.Copy(filePath, backupPath)
                            End If
                        Catch ex As Exception
                            Log(String.Format(Loc.GetString("LogError"), "Backup failed: " & ex.Message))
                        End Try
                    End If

                    Dim tempPath = filePath & ".tmp"
                    Dim savedTemp As Boolean = False
                    
                    Try
                        If isBundle Then
                            For Each kvp In assetInsts
                                Dim j = kvp.Key
                                Dim inst = kvp.Value
                                Using memStream As New MemoryStream()
                                    Using writer As New AssetsFileWriter(memStream)
                                        inst.file.Write(writer)
                                    End Using
                                    bunInst.file.BlockAndDirInfo.DirectoryInfos(j).Replacer = New ContentReplacerFromBuffer(memStream.ToArray())
                                End Using
                            Next
                            
                            Using fs As FileStream = File.Create(tempPath)
                                Using writer As New AssetsFileWriter(fs)
                                    bunInst.file.Write(writer)
                                End Using
                            End Using
                        Else
                            Dim inst = assetInsts(0)
                            Using fs As FileStream = File.Create(tempPath)
                                Using writer As New AssetsFileWriter(fs)
                                    inst.file.Write(writer)
                                End Using
                            End Using
                        End If
                        savedTemp = True
                    Catch ex As UnauthorizedAccessException
                        Log(String.Format(Loc.GetString("LogError"), "Access Denied to " & Path.GetFileName(filePath)))
                    Catch ex As Exception
                        Log(String.Format(Loc.GetString("LogError"), "Failed to write temp file for: " & Path.GetFileName(filePath) & " - " & ex.Message))
                    End Try

                    ' Ensure all read handles are closed before attempting to overwrite
                    For Each kvp In assetInsts
                        Try
                            kvp.Value.file.Close()
                        Catch
                        End Try
                    Next
                    If isBundle AndAlso bunInst IsNot Nothing Then
                        Try
                            bunInst.file.Close()
                        Catch
                        End Try
                    End If
                    assetInsts.Clear()
                    bunInst = Nothing
                    
                    am.UnloadAllAssetsFiles(True)
                    am.UnloadAllBundleFiles()

                    If savedTemp Then
                        Try
                            If ValidateAssetFile(tempPath) Then
                                File.Copy(tempPath, filePath, True)
                                File.Delete(tempPath)
                                Log(String.Format(Loc.GetString("LogSaved"), Path.GetFileName(filePath)))
                            Else
                                Log(String.Format(Loc.GetString("LogError"), "Validation failed; original file was not overwritten: " & Path.GetFileName(filePath)))
                                Try
                                    File.Delete(tempPath)
                                Catch
                                End Try
                            End If
                        Catch ex As Exception
                            Log(String.Format(Loc.GetString("LogError"), "Failed to overwrite original file: " & Path.GetFileName(filePath) & " - " & ex.Message))
                        End Try
                    End If
                End If

            Catch ex As Exception
                Log($"[✘] Failed to process or save {filePath}: {ex.Message}")
            End Try
            
            ' Unload file data only - preserve ClassPackage/ClassDatabase cache
            Try
                am.UnloadAllAssetsFiles(True)
            Catch
            End Try
            Try
                am.UnloadAllBundleFiles()
            Catch
            End Try
    End Sub

    Private Sub SaveRawPatchedFile(filePath As String, records As List(Of RawPatchRecord), backup As Boolean)
        If records Is Nothing OrElse records.Count = 0 Then
            Log(String.Format(Loc.GetString("LogError"), "Raw patch requested but no patch records were produced: " & Path.GetFileName(filePath)))
            Return
        End If

        If backup Then
            Dim backupPath = filePath & ".bak"
            Try
                If Not File.Exists(backupPath) Then
                    Log(String.Format(Loc.GetString("LogBackup"), Path.GetFileName(backupPath)))
                    File.Copy(filePath, backupPath)
                End If
            Catch ex As Exception
                Log(String.Format(Loc.GetString("LogError"), "Backup failed: " & ex.Message))
                Return
            End Try
        End If

        Dim tempPath = filePath & ".tmp"
        Try
            Dim originalLength = New FileInfo(filePath).Length
            File.Copy(filePath, tempPath, True)

            Using fs As New FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
                If fs.Length <> originalLength Then
                    Throw New InvalidDataException("Temp file length changed before patching.")
                End If

                For Each record In records
                    If record.ByteSize <= 0 OrElse record.ModifiedBytes Is Nothing Then
                        Throw New InvalidDataException($"Invalid raw patch record for pathId:{record.PathId}.")
                    End If
                    If record.ModifiedBytes.Length > record.ByteSize Then
                        Throw New InvalidDataException($"Raw patch would grow shader data for pathId:{record.PathId}.")
                    End If
                    If record.ChangedValueCount <= 0 Then
                        Throw New InvalidDataException($"No changed colMask values for pathId:{record.PathId}.")
                    End If

                    Dim originalBytes(record.ByteSize - 1) As Byte
                    fs.Position = record.AbsoluteOffset
                    ReadFully(fs, originalBytes, 0, originalBytes.Length)

                    Dim patchIndexes As New List(Of Integer)()
                    Dim compareLength = Math.Min(originalBytes.Length, record.ModifiedBytes.Length)
                    For i As Integer = 0 To compareLength - 1
                        If originalBytes(i) <> record.ModifiedBytes(i) Then
                            If record.ModifiedBytes(i) <> 0 Then
                                Throw New InvalidDataException($"Raw patch found a non-zero replacement byte for pathId:{record.PathId} at +0x{i:X}.")
                            End If
                            patchIndexes.Add(i)
                        End If
                    Next

                    Dim maxExpectedPatchBytes = record.ChangedValueCount * 4
                    If patchIndexes.Count = 0 OrElse patchIndexes.Count > maxExpectedPatchBytes Then
                        Throw New InvalidDataException($"Raw patch diff looked unsafe for pathId:{record.PathId}. diffBytes={patchIndexes.Count}, changedValues={record.ChangedValueCount}.")
                    End If

                    For Each index In patchIndexes
                        fs.Position = record.AbsoluteOffset + index
                        fs.WriteByte(record.ModifiedBytes(index))
                    Next

                    Log($"   -> raw patched {patchIndexes.Count} bytes for {record.ShaderName} (pathId:{record.PathId})")
                Next
            End Using

            If New FileInfo(tempPath).Length <> New FileInfo(filePath).Length Then
                Throw New InvalidDataException("Raw patch changed the file length.")
            End If

            If ValidateAssetFile(tempPath) Then
                File.Copy(tempPath, filePath, True)
                File.Delete(tempPath)
                Log(String.Format(Loc.GetString("LogSaved"), Path.GetFileName(filePath)))
            Else
                Throw New InvalidDataException("Raw patched file failed validation.")
            End If
        Catch ex As Exception
            Log(String.Format(Loc.GetString("LogError"), "Raw patch failed; original file was not overwritten: " & Path.GetFileName(filePath) & " - " & ex.Message))
            Try
                If File.Exists(tempPath) Then File.Delete(tempPath)
            Catch
            End Try
        End Try
    End Sub

    Private Sub ReadFully(stream As Stream, buffer As Byte(), offset As Integer, count As Integer)
        Dim total = 0
        While total < count
            Dim read = stream.Read(buffer, offset + total, count - total)
            If read = 0 Then
                Throw New EndOfStreamException("Unexpected end of stream while reading raw patch bytes.")
            End If
            total += read
        End While
    End Sub

    Public Sub ExecuteReplacement(targetInfo As ShaderInfo, sourceInfo As ShaderInfo, backup As Boolean)
        Log(String.Format(Loc.GetString("LogReplacing"), Path.GetFileName(targetInfo.FilePath)))

        Dim am As New AssetsManager()
        If File.Exists("classdata.tpk") Then
            Try
                am.LoadClassPackage("classdata.tpk")
            Catch
            End Try
        End If
        
        Try
            Dim sourceInst = am.LoadAssetsFile(sourceInfo.FilePath, False)
            If am.ClassDatabase Is Nothing AndAlso sourceInst.file.Metadata.TypeTreeEnabled = False AndAlso File.Exists("classdata.tpk") Then
                am.LoadClassDatabaseFromPackage(sourceInst.file.Metadata.UnityVersion)
            End If
            Dim sourceAssetInfo = sourceInst.file.GetAssetInfo(sourceInfo.PathId)
            If sourceAssetInfo Is Nothing Then
                Log("    -> [Error] Source shader not found.")
                Return
            End If
            
            Dim sourceBaseField = am.GetBaseField(sourceInst, sourceAssetInfo)
            
            Dim targetInst = am.LoadAssetsFile(targetInfo.FilePath, False)
            If am.ClassDatabase Is Nothing AndAlso targetInst.file.Metadata.TypeTreeEnabled = False AndAlso File.Exists("classdata.tpk") Then
                am.LoadClassDatabaseFromPackage(targetInst.file.Metadata.UnityVersion)
            End If
            If RequiresRawPatchWriter(targetInst) Then
                Log(String.Format(Loc.GetString("LogError"), "Unsafe replacement save blocked for Unity 6000 asset. Use raw in-place patching instead: " & Path.GetFileName(targetInfo.FilePath)))
                Return
            End If
            Dim targetAssetInfo = targetInst.file.GetAssetInfo(targetInfo.PathId)
            If targetAssetInfo Is Nothing Then
                Log("    -> [Error] Target shader not found.")
                Return
            End If
            
            Dim targetBaseField = am.GetBaseField(targetInst, targetAssetInfo)
            Dim originalName = targetBaseField.Get("m_Name").AsString
            If String.IsNullOrWhiteSpace(originalName) Then
                Try
                    originalName = targetBaseField.Get("m_ParsedForm").Get("m_Name").AsString
                Catch
                End Try
            End If

            ' Apply original name to source typetree
            sourceBaseField.Get("m_Name").AsString = originalName
            Dim parsedForm = sourceBaseField.Get("m_ParsedForm")
            If Not parsedForm.IsDummy() Then
                Dim pfName = parsedForm.Get("m_Name")
                If Not pfName.IsDummy() Then
                    pfName.AsString = originalName
                End If
            End If

            Dim bytes = sourceBaseField.WriteToByteArray()
            targetAssetInfo.Replacer = New ContentReplacerFromBuffer(bytes)
            
            If backup Then
                Dim backupPath = targetInfo.FilePath & ".bak"
                Try
                    If Not File.Exists(backupPath) Then
                        Log(String.Format(Loc.GetString("LogBackup"), Path.GetFileName(backupPath)))
                        File.Copy(targetInfo.FilePath, backupPath)
                    End If
                Catch ex As Exception
                    Log(String.Format(Loc.GetString("LogError"), "Backup failed: " & ex.Message))
                End Try
            End If

            Dim tempPath = targetInfo.FilePath & ".tmp"
            Using fs As FileStream = File.Create(tempPath)
                Using writer As New AssetsFileWriter(fs)
                    targetInst.file.Write(writer)
                End Using
            End Using

            Try
                targetInst.file.Close()
            Catch
            End Try
            Try
                sourceInst.file.Close()
            Catch
            End Try
            am.UnloadAllAssetsFiles(True)
            am.UnloadAllBundleFiles()

            If ValidateAssetFile(tempPath) Then
                File.Copy(tempPath, targetInfo.FilePath, True)
                File.Delete(tempPath)
            Else
                Log(String.Format(Loc.GetString("LogError"), "Validation failed; original file was not overwritten: " & Path.GetFileName(targetInfo.FilePath)))
                Try
                    File.Delete(tempPath)
                Catch
                End Try
                Return
            End If
            
            Log($"   -> Replaced content of '{originalName}'")
            Log(String.Format(Loc.GetString("LogSaved"), Path.GetFileName(targetInfo.FilePath)))

        Catch ex As Exception
            Log(String.Format(Loc.GetString("LogError"), "Replacement failed: " & ex.Message))
        Finally
            Try
                am.UnloadAll()
            Catch
            End Try
        End Try
    End Sub

    Private Function RequiresRawPatchWriter(assetInsts As Dictionary(Of Integer, AssetsFileInstance)) As Boolean
        For Each kvp In assetInsts
            If RequiresRawPatchWriter(kvp.Value) Then Return True
        Next
        Return False
    End Function

    Private Function RequiresRawPatchWriter(inst As AssetsFileInstance) As Boolean
        If inst Is Nothing OrElse inst.file Is Nothing OrElse inst.file.Metadata Is Nothing Then Return False
        Return inst.file.Metadata.UnityVersion.StartsWith("6000.") AndAlso inst.file.Metadata.TypeTreeEnabled = False
    End Function

    Private Function ValidateAssetFile(filePath As String) As Boolean
        Dim am As New AssetsManager()
        Try
            If File.Exists("classdata.tpk") Then
                Try
                    am.LoadClassPackage("classdata.tpk")
                Catch
                End Try
            End If

            Dim isBundle = False
            Using fs As FileStream = File.OpenRead(filePath)
                If fs.Length > 20 Then
                    Dim br As New BinaryReader(fs)
                    Dim magic As String = New String(br.ReadChars(7))
                    If magic.StartsWith("UnityFS") OrElse magic.StartsWith("UnityWeb") OrElse magic.StartsWith("UnityRaw") Then
                        isBundle = True
                    End If
                End If
            End Using

            Dim assetInsts As New List(Of AssetsFileInstance)()
            If isBundle Then
                Dim bunInst = am.LoadBundleFile(filePath, False)
                For j As Integer = 0 To bunInst.file.BlockAndDirInfo.DirectoryInfos.Count - 1
                    Dim dirInfo = bunInst.file.BlockAndDirInfo.DirectoryInfos(j)
                    If dirInfo.Name.EndsWith(".resS") OrElse dirInfo.Name.EndsWith(".resource") Then Continue For
                    Try
                        Dim assetInst = am.LoadAssetsFileFromBundle(bunInst, j, False)
                        If assetInst IsNot Nothing Then assetInsts.Add(assetInst)
                    Catch
                    End Try
                Next
            Else
                Dim inst = am.LoadAssetsFile(filePath, False)
                If inst Is Nothing OrElse inst.file Is Nothing Then Return False
                assetInsts.Add(inst)
            End If

            If assetInsts.Count = 0 Then Return False

            For Each inst In assetInsts
                If am.ClassDatabase Is Nothing AndAlso inst.file.Metadata.TypeTreeEnabled = False AndAlso File.Exists("classdata.tpk") Then
                    Try
                        am.LoadClassDatabaseFromPackage(inst.file.Metadata.UnityVersion)
                    Catch
                    End Try
                End If

                For Each info In inst.file.GetAssetsOfType(AssetClassID.Shader)
                    Dim baseField = am.GetBaseField(inst, info)
                    If baseField Is Nothing OrElse baseField.IsDummy() Then Return False
                Next
            Next

            Return True
        Catch
            Return False
        Finally
            Try
                am.UnloadAll()
            Catch
            End Try
        End Try
    End Function

    Public Sub RepackAndSignApk(originalApkPath As String, tempDir As String, modifiedFiles As List(Of String))
        Dim dir = Path.GetDirectoryName(originalApkPath)
        Dim baseName = Path.GetFileNameWithoutExtension(originalApkPath)
        Dim ext = Path.GetExtension(originalApkPath)
        Dim outputApkPath = Path.Combine(dir, $"{baseName}_mod{ext}")
        Dim unsignedApkPath = Path.Combine(Path.GetTempPath(), $"temp_{Path.GetFileName(originalApkPath)}")

        Try
            Dim modifiedFilesMap As New Dictionary(Of String, String)
            For Each f In modifiedFiles
                Dim relPath = Path.GetRelativePath(tempDir, f).Replace("\", "/")
                modifiedFilesMap(relPath) = f
            Next

            Log(String.Format(Loc.GetString("LogRebuilding"), modifiedFilesMap.Count))
            Using zin = ZipFile.OpenRead(originalApkPath)
                Using zout = ZipFile.Open(unsignedApkPath, ZipArchiveMode.Create)
                    For Each item In zin.Entries
                        If modifiedFilesMap.ContainsKey(item.FullName) Then
                            Dim modifiedFilePath = modifiedFilesMap(item.FullName)
                            Dim compressType = If(item.FullName.ToLower().EndsWith(".so"), CompressionLevel.NoCompression, CompressionLevel.Optimal)
                            zout.CreateEntryFromFile(modifiedFilePath, item.FullName, compressType)
                            Log($"  -> Updated: {item.FullName}")
                        Else
                            ' Copy original entry
                            Dim newEntry = zout.CreateEntry(item.FullName)
                            Using inStream = item.Open()
                                Using outStream = newEntry.Open()
                                    inStream.CopyTo(outStream)
                                End Using
                            End Using
                        End If
                    Next
                End Using
            End Using

            Log(Loc.GetString("LogSigning"))
            
            ' Find uber-apk-signer.jar
            Dim exeDir = AppDomain.CurrentDomain.BaseDirectory
            Dim jarPath = Path.Combine(exeDir, "uber-apk-signer.jar")
            If Not File.Exists(jarPath) Then
                Dim jars = Directory.GetFiles(exeDir, "uber-apk-signer*.jar")
                If jars.Length > 0 Then
                    jarPath = jars(0)
                Else
                    Throw New Exception($"uber-apk-signer.jar not found in {exeDir}")
                End If
            End If

            Dim apkDir = Path.GetDirectoryName(unsignedApkPath)
            Dim psi As New ProcessStartInfo("java", $"-Dfile.encoding=UTF-8 -jar ""{jarPath}"" -a ""{unsignedApkPath}"" --out ""{apkDir}""")
            psi.RedirectStandardOutput = True
            psi.RedirectStandardError = True
            psi.UseShellExecute = False
            psi.CreateNoWindow = True
            psi.WorkingDirectory = apkDir

            Using proc = Process.Start(psi)
                proc.WaitForExit()
                If proc.ExitCode <> 0 Then
                    Dim err = proc.StandardError.ReadToEnd()
                    Dim out = proc.StandardOutput.ReadToEnd()
                    Throw New Exception($"uber-apk-signer failed with exit code {proc.ExitCode}:{vbCrLf}{err}{vbCrLf}{out}")
                End If
            End Using

            Dim unsignedBaseName = Path.GetFileNameWithoutExtension(unsignedApkPath)
            Dim signedApkAligned = Path.Combine(apkDir, $"{unsignedBaseName}-aligned-debugSigned.apk")

            If File.Exists(outputApkPath) Then File.Delete(outputApkPath)
            File.Move(signedApkAligned, outputApkPath)
            Log(String.Format(Loc.GetString("LogSignedApk"), Path.GetFileName(outputApkPath)))

            Try
                File.Delete(unsignedApkPath)
            Catch
            End Try
        Catch ex As Exception
            Log(String.Format(Loc.GetString("LogError"), "Repack failed: " & ex.Message))
            Throw
        End Try
    End Sub
End Class
