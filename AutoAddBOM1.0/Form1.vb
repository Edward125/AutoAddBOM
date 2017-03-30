Imports System.IO
Imports System.Collections.ObjectModel


'*******************************
'用txtFolder文本框輸入BOM所在的文件夾
'用txtResult選擇BOM比對后的結果檔案
'******************************


Public Class frmAutoAddBom

    Private Sub txtBom_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtBom.DoubleClick
        Dim strfoldername As String
        FolderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer
        FolderBrowserDialog1.ShowNewFolderButton = True
        If FolderBrowserDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
            strfoldername = FolderBrowserDialog1.SelectedPath
            txtBom.Text = strfoldername
        End If
    End Sub

    Private Sub txtResult_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtResult.DoubleClick
        Dim myStream As Stream = Nothing
        Dim openFileDialog1 As New OpenFileDialog()

        '   openFileDialog1.InitialDirectory = "c:\"
        openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*"
        openFileDialog1.FilterIndex = 2
        openFileDialog1.RestoreDirectory = False

        If openFileDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            txtResult.Text = openFileDialog1.FileName
        End If
    End Sub


    Private Sub btnGo_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGo.Click

        If txtBom.Text <> "" Then

            Dim path As String = txtBom.Text & "\"
            Dim searchPattern As String = "*.txt"   '通过文件名称以及扩展名特点,筛选出指定文件

            Dim di As DirectoryInfo = New DirectoryInfo(path)
            Dim directories() As DirectoryInfo = _
                di.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly)
            Dim files() As FileInfo = _
                di.GetFiles(searchPattern, SearchOption.TopDirectoryOnly)
            Dim bomFile As FileInfo
            Dim i As Integer = 1
            Dim j As Integer = 1
            Dim LineOfText As String = ""
            Dim StreamToWrite As StreamWriter
            Dim BomLabel As String = "" 'UMA or DIS
            StreamToWrite = My.Computer.FileSystem.OpenTextFileWriter(Application.StartupPath & "\BeginSection.txt", False)


            '********************write section 1**************************************
            'print tab(5);" (1)  is   5544L01001G "
            '*************************************************************************
            For Each bomFile In files

                'copy boms
                Try
                    My.Computer.FileSystem.CopyFile(bomFile.FullName, Application.StartupPath & "\AutoAddBOM\BOM\" & bomFile.Name) 'copy boms
                Catch ex As Exception
                    MessageBox.Show(ex.Message)
                End Try



                '根據文件大小,重新分類BOM
                Dim size As Integer = Int(bomFile.Length / 1024) + 1
                CreatFolder(Application.StartupPath & "\AutoAddBOM\BOM\" & size)

                Try
                    My.Computer.FileSystem.CopyFile(Application.StartupPath & "\AutoAddBOM\BOM\" & bomFile.Name, Application.StartupPath & "\AutoAddBOM\BOM\" & size & "\" & bomFile.Name) 'copy boms
                Catch ex As Exception
                    MessageBox.Show(ex.Message)
                End Try




                '判斷是uma還是dis
                Try
                    Dim fileContents As String
                    fileContents = My.Computer.FileSystem.ReadAllText(Application.StartupPath & "\AutoAddBOM\BOM\" & bomFile.Name)
                    If fileContents.Contains("VRAM") Then
                        BomLabel = "DIS"
                    Else
                        BomLabel = "UMA"
                    End If
                    'delete bom
                    My.Computer.FileSystem.DeleteFile(Application.StartupPath & "\AutoAddBOM\BOM\" & bomFile.Name, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin)
                Catch ex As Exception
                    MessageBox.Show(ex.Message)
                End Try
                '格式化料號名稱
                Dim bomName As String
                bomName = bomFile.Name.Replace(".txt", "")
                bomName = bomName.Replace("M", "0")
                bomName = bomName & "    " & BomLabel
                LineOfText = "print tab(5); " & """" & "(" & i & ")  is " & bomName & """"
                StreamToWrite.WriteLine(LineOfText)
                i += 1
            Next bomFile
            '  StreamToWrite.Close()

            LineOfText = vbCrLf & "input  " & """" & "Please select Panel Version!" & """" & ",TmpVer$"
            StreamToWrite.WriteLine(LineOfText)
            '***********************write section 2*****************************
            'if TmpVer$="1" then
            '    Panel_Rev$ = "5544L01001G"
            '    Panel_SN$ = "5544L01001G"
            '  execute("title '5544L01001G'")
            'end if
            '*******************************************************************
            For Each bomFile In files
                Dim bomName As String
                bomName = bomFile.Name.Replace(".txt", "")
                bomName = bomName.Replace("M", "0")
                LineOfText = vbCrLf & "if TmpVer$=" & """" & j & """" & " then " & vbCrLf & "    Panel_Rev$=" & """" & bomName & """" & vbCrLf & "    Panel_SN$=" & """" & bomName & """" & vbCrLf & "  execute " & """" & "title '" & bomName & "'""" & vbCrLf & "end if " & vbCrLf
                StreamToWrite.WriteLine(LineOfText)
                j += 1
            Next bomFile
            '   StreamToWrite.Close()

            '***********************write section 3 *******************************
            'if  TmpVer$<>"1" and  TmpVer$<>"2"  and  TmpVer$<>"3" then  goto ReinputVER
            '*********************************************************************


            LineOfText = "if TmpVer$<>" & """" & "1" & """"
            For k As Integer = 2 To i - 2
                LineOfText = LineOfText & " and TmpVer$<>" & """" & k & """"
            Next
            LineOfText = LineOfText & " and TmpVer$<>" & """" & (i - 1) & """" & " then goto ReinputVER"
            StreamToWrite.WriteLine(LineOfText)
            StreamToWrite.Close()

            MsgBox("BeginSection.txt" & " Saves in " & My.Computer.FileSystem.CurrentDirectory)

        End If

    End Sub


    Private Sub btnGo2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGo2.Click

        If txtResult.Text <> "" Then

            Dim textStream As StreamReader
            Dim lineStr As String = ""
            Dim containAS As Boolean = False
            Dim lines As Integer = 0
            Dim LineOfText As String = "if"
            Dim StreamToWrite As StreamWriter
            StreamToWrite = My.Computer.FileSystem.OpenTextFileWriter(Application.StartupPath & "\AnalogSection.txt", False)
            textStream = My.Computer.FileSystem.OpenTextFileReader(txtResult.Text)

            Do Until textStream.EndOfStream  '从文件逐行读取
                lineStr = textStream.ReadLine '将读到的文本赋给linestr
                ' lineStr = "if"
                If lineStr <> "" And lineStr.Contains(",") Then  '判斷一行中是否還有多個(2個及其以上)的料號
                    Dim bom() As String
                    lineStr = lineStr.Replace("M", "0")
                    bom = lineStr.Split(",")
                    For k As Integer = 0 To UBound(bom)
                        If k <> UBound(bom) Then
                            LineOfText = LineOfText & " Panel_SN$=" & """" & bom(k) & """" & " or" '如果不是最後一個料號,每個料號后需要加"or"
                        Else
                            LineOfText = LineOfText & " Panel_SN$=" & """" & bom(k) & """" & " then" & vbCrLf & "  print tab(5);" & """" & "Testing " & bom(0) & "..." & """" & vbCrLf & " end if " & vbCrLf '最後一個料號,料號后不需要加"or"
                        End If
                        '  LineOfText = LineOfText & " then " & vbCrLf
                    Next
                    StreamToWrite.WriteLine(LineOfText)
                    LineOfText = "if"
                ElseIf lineStr <> "" Then '一行中只含有一個料號
                    lineStr = lineStr.Replace("M", "0")
                    LineOfText = LineOfText & " Panel_SN$=" & """" & lineStr & """" & " then" & vbCrLf & "  print tab(5);" & """" & "Testing " & lineStr & "..." & """" & vbCrLf & " end if " & vbCrLf
                    StreamToWrite.WriteLine(LineOfText)
                    LineOfText = "if"
                End If
                lines += 1
            Loop
            StreamToWrite.Close()
            MsgBox("AnalogSection.txt" & " Saves in " & Application.StartupPath)
        End If
    End Sub

    Private Sub Label1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label1.Click

    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        txtBom.Focus()
        CreatFolder(Application.StartupPath & "\AutoAddBOM")
    End Sub


    Private Sub CreatFolder(ByVal FolderFullPath As String)
        Dim folderExists As Boolean
        folderExists = My.Computer.FileSystem.DirectoryExists(FolderFullPath)
        If folderExists = False Then
            My.Computer.FileSystem.CreateDirectory(FolderFullPath)
        End If
    End Sub

End Class
