Imports System
Imports EnvDTE
Imports EnvDTE80
Imports EnvDTE90
Imports EnvDTE90a
Imports EnvDTE100
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Text.RegularExpressions

Public Module GitHubMacros

    'Show the file contents on GitHub
    Sub ShowFile()
        showOnGitHub("blob")
    End Sub

    'Show the commit history of the file on GitHub
    Sub ShowLog()
        showOnGitHub("commits")
    End Sub

    'Show the line by line history of the file on GitHub
    Sub ShowBlame()
        showOnGitHub("blame")
    End Sub



    Private Sub showOnGitHub(ByVal page As String)
        Dim filePath As String
        Dim gitRoot As String
        Dim relativePath As String
        Dim githubRootPath As String
        Dim githubFileUrl As String

        Dim ActiveDoc As Document = DTE.ActiveDocument
        If ActiveDoc Is Nothing Then Return
        filePath = ActiveDoc.FullName
        gitRoot = findGitRepoRoot(filePath)
        If gitRoot Is Nothing Then
            MsgBox("Not a git repository")
            Return
        End If
        relativePath = filePath.Substring(gitRoot.Length + 1).Replace("\", "/")

        Dim repo = New GitRepo(gitRoot)
        Dim gitHubRemote = (From r In repo.GetRemotes() Where r.Url.Contains("github.com")).FirstOrDefault()
        If gitHubRemote Is Nothing Then
            MsgBox("No github remote found")
            Return
        End If

        githubRootPath = buildGitHubBaseUrl(gitHubRemote.Url)
        If githubRootPath Is Nothing Then
            MsgBox("Unrecognized github repository url format: " + gitHubRemote.Url)
            Return
        End If

        Dim branchName As String = "master" 'TODO: discover this
        githubFileUrl = String.Format("{0}/{1}/{2}/{3}", githubRootPath, page, branchName, relativePath)

        System.Diagnostics.Process.Start(githubFileUrl)
    End Sub

    Private Function findGitRepoRoot(ByVal filePath As String)
        Dim currentDirectory As String = filePath

        Do
            currentDirectory = Path.GetDirectoryName(currentDirectory)
            If Directory.Exists(Path.Combine(currentDirectory, GitRepo.GIT_REPO_FOLDER)) Then Return currentDirectory
        Loop Until Path.GetPathRoot(currentDirectory) = currentDirectory
        Return Nothing
    End Function

    Private Function buildGitHubBaseUrl(ByVal remoteUrl As String)
        Dim patterns = New String() {"git://github\.com/(?<username>(\w|-)+)/(?<project>(\w|-)+)\.git", _
                                     "git@github\.com:(?<username>(\w|-)+)/(?<project>(\w|-)+)\.git", _
                                     "https://(\w|-)+@github\.com/(?<username>(\w|-)+)/(?<project>(\w|-)+)\.git"}

        Dim pattern As Match = (From p In patterns Select Regex.Match(remoteUrl, p)).FirstOrDefault(Function(x) x.Success)
        If (pattern Is Nothing) Then Return Nothing
        Return String.Format("http://github.com/{0}/{1}", pattern.Groups("username").Value, pattern.Groups("project").Value)
    End Function
End Module


Public Class GitRepo
    Public Const GIT_REPO_FOLDER As String = ".git"
    Private repoFolder As String
    Private configParsed As Boolean
    Private remotes As List(Of Remote)
    Private configSections As IList(Of ConfigSection) = New List(Of ConfigSection)

    Public Sub New(ByVal repoRoot As String)
        repoFolder = Path.Combine(repoRoot, GIT_REPO_FOLDER)
    End Sub

    Public Function GetRemotes() As IEnumerable(Of Remote)
        parseConfig()
        Return From section In configSections Where section.Kind = "remote" Select New Remote(section.Name, section.Values("url"))
    End Function

    Private Sub parseConfig()
        If configParsed Then Return
        Dim match As Match
        Dim parts As String()
        Dim currentSection As ConfigSection
        Const pattern = "^\[(?<kind>\w+)(\s+""(?<name>\w+)"")?\]"
        For Each line In File.ReadLines(Path.Combine(repoFolder, "config"))
            match = System.Text.RegularExpressions.Regex.Match(line, pattern)
            If (match.Success) Then
                currentSection = New ConfigSection
                configSections.Add(currentSection)
                currentSection.Kind = match.Groups("kind").Value
                currentSection.Name = match.Groups("name").Value
            Else
                parts = line.Split(New String() {"="}, 2, StringSplitOptions.RemoveEmptyEntries)
                If (parts.Length > 1) Then
                    currentSection.Values.Add(parts(0).Trim, parts(1).Trim)
                End If
            End If
        Next
        configParsed = True
    End Sub
End Class

Public Class ConfigSection
    Public Kind As String
    Public Name As String
    Public Values As IDictionary(Of String, String) = New Dictionary(Of String, String)
End Class


Public Class Remote
    Public Sub New(ByVal newName As String, ByVal newUrl As String)
        Name = newName
        Url = newUrl
    End Sub
    Public Name As String
    Public Url As String
End Class
