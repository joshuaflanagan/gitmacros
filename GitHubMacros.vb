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
        showOnGitHub("blame", True)
    End Sub

    'Show details for a provided Issue number
    Sub ShowIssue()
        Dim gitRoot As String = findGitRepoRoot(DTE.ActiveDocument.FullName)
        If gitRoot Is Nothing Then
            MsgBox("Not a git repository")
            Return
        End If

        Dim issueNumber As String
        issueNumber = InputBox("Issue number" & vbCrLf & "(leave empty to see all issues)", "Show GitHub Issue")
        Dim githubRootPath As String = findBaseGitHubUrlForLocalRepo(gitRoot)

        Dim githubFilePath As String
        Dim issuePart As String = If(issueNumber = "", String.Empty, "#issue/" + issueNumber)
        githubFilePath = String.Format("{0}/issues{1}", githubRootPath, issuePart)
        System.Diagnostics.Process.Start(githubFilePath)

    End Sub


    Private Sub showOnGitHub(ByVal page As String, Optional ByVal goToLineNumber As Boolean = False)
        Dim filePath As String
        Dim gitRoot As String
        Dim relativePath As String
        Dim githubFileUrl As String

        Dim ActiveDoc As Document = DTE.ActiveDocument
        If ActiveDoc Is Nothing Then Return
        filePath = ActiveDoc.FullName
        gitRoot = findGitRepoRoot(filePath)
        If gitRoot Is Nothing Then
            MsgBox("Not a git repository")
            Return
        End If

        Dim githubRootPath As String = findBaseGitHubUrlForLocalRepo(gitRoot)

        Dim branchName As String = findBranchForLocalRepo(gitRoot)
        relativePath = filePath.Substring(gitRoot.Length + 1).Replace("\", "/")
        githubFileUrl = String.Format("{0}/{1}/{2}/{3}", githubRootPath, page, branchName, relativePath)

        ' add anchor (for the current line number) to the GitHub URL
        If goToLineNumber Then
            Dim lineNumber As Integer? = GetCurrentLineNumber()
            If lineNumber.HasValue Then
                githubFileUrl &= "#LID" & Math.Max(1, lineNumber.Value - 5)
            End If
        End If

        System.Diagnostics.Process.Start(githubFileUrl)
    End Sub

    Private Function findBaseGitHubUrlForLocalRepo(ByVal gitRoot As String)
        Dim githubRootPath As String

        Dim repo = New GitRepo(gitRoot)
        Dim remotes As String = ""
        Dim gitHubRemote = (From r In repo.GetRemotes() Where r.Url.Contains(GitHubServer)).FirstOrDefault()
        If gitHubRemote Is Nothing Then
            MsgBox("No github remote found")
            Return Nothing
        End If
        githubRootPath = buildGitHubBaseUrl(gitHubRemote.Url)
        If githubRootPath Is Nothing Then
            MsgBox("Unrecognized github repository url format: " + gitHubRemote.Url)
            Return Nothing
        End If
        Return githubRootPath
    End Function

    Private Function findBranchForLocalRepo(ByVal gitRoot As String)
        Dim repo = New GitRepo(gitRoot)
        Return repo.GetBranch()
    End Function

    Private Function findGitRepoRoot(ByVal filePath As String)
        Dim currentDirectory As String = filePath

        Do
            currentDirectory = Path.GetDirectoryName(currentDirectory)
            If Directory.Exists(Path.Combine(currentDirectory, GitRepo.GIT_REPO_FOLDER)) Then Return currentDirectory
        Loop Until Path.GetPathRoot(currentDirectory) = currentDirectory
        Return Nothing
    End Function

    Private Function buildGitHubBaseUrl(ByVal remoteUrl As String)
        Dim server As String = Regex.Escape(GitHubServer)
        Dim patterns = New String() {"git://" & server & "/(?<username>(\w|-)+)/(?<project>(\w|-)+)\.git", _
                                     "git@" & server & ":(?<username>(\w|-)+)/(?<project>(\w|-)+)\.git", _
                                     "https://(\w|-)+@" & server & "/(?<username>(\w|-)+)/(?<project>(\w|-)+)\.git"}

        Dim pattern As Match = (From p In patterns Select Regex.Match(remoteUrl, p)).FirstOrDefault(Function(x) x.Success)
        If (pattern Is Nothing) Then Return Nothing
        Return String.Format("http://" & GitHubServer & "/{0}/{1}", pattern.Groups("username").Value, pattern.Groups("project").Value)
    End Function

    Private Function GetCurrentLineNumber() As Integer?
        Dim textDocument As EnvDTE.TextDocument = CType(DTE.ActiveDocument.Object, EnvDTE.TextDocument)
        If textDocument Is Nothing Then Return Nothing
        Return textDocument.Selection.ActivePoint.Line
    End Function

    Const GitHubServer As String = "github.com"
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

    Public Function GetBranch() As String
        Dim head As String = File.ReadAllText(Path.Combine(repoFolder, "HEAD")).Trim()
        Dim match As Match = Regex.Match(head, "^ref: refs/heads/(.*)$")
        If match.Success Then Return match.Groups(1).Value Else Return "master"
    End Function

    Private Sub parseConfig()
        If configParsed Then Return
        Dim match As Match
        Dim parts As String()
        Dim currentSection As ConfigSection
        Const pattern = "^\[(?<kind>[a-zA-Z_0-9\-\.]+)(\s+""(?<name>[a-zA-Z_0-9\-\.]+)"")?\]"
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
