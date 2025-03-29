using System.Linq;
using System.Text.RegularExpressions;
using Fastenshtein;

public class FilterManager(string filter, bool considerFolderContents, bool regex, bool typoTolerance, bool anyOrder)
{
    private readonly string _filter = filter.ToLower();
    public void ApplyFilter(List<FileInfo> fileInfos)
    {
        fileInfos.RemoveAll(file => !PassesFilter(file));
    }

    private bool PassesFilter(FileInfo file)
    {
        string fileName = file.Name.ToLower();
        if (regex)
        {
            if (Regex.IsMatch(fileName, _filter)) {
                return true;
            }
        }
        else
        {
            if (fileName.Contains(_filter))
            {
                return true;
            }
            if (typoTolerance)
            {
                if (Levenshtein.Distance(_filter, fileName) <= 1)
                {
                    return true;
                }
                string[] fileNameTokens = System.Text.RegularExpressions.Regex.Split(fileName, @"\W+");
                string[] filterTokens = _filter.Split(' ', StringSplitOptions.RemoveEmptyEntries); ;
                if (anyOrder)
                {
                    var anyNotFound = false;
                    foreach (var filterToken in filterTokens)
                    {
                        bool found = fileNameTokens.Any(z => z == filterToken || Levenshtein.Distance(z, filterToken) <= 1);
                        if (!found)
                        {
                            anyNotFound = true;
                        }
                    }
                    if (anyNotFound == false)
                    {
                        return true;
                    }
                }

                for (var i = 0; i + filterTokens.Length - 1 < fileNameTokens.Length; i++)
                {
                    var tempFileName = String.Join(' ', fileNameTokens.Skip(i).Take(filterTokens.Length));
                    if (_filter == tempFileName || Levenshtein.Distance(_filter, tempFileName) <= 1)
                    {
                        return true;
                    }
                }
                if (fileName.Length > _filter.Length)
                {
                    var tempFileName = fileName.Substring(0, _filter.Length);
                    if (_filter == tempFileName || Levenshtein.Distance(_filter, tempFileName) <= 1)
                    {
                        return true;
                    }
                }
            }
            else //!typoTolerance
            {
                if (anyOrder)
                {
                    string[] filterTokens = anyOrder ? _filter.Split(' ') : new[] { _filter };
                    bool allMatches = filterTokens.All(token => fileName.Contains(token));
                    if (allMatches)
                    {
                        return true;
                    }
                }
            } //end !typoTolerance
        } //end !regex



        if (considerFolderContents && file.Attributes.HasFlag(FileAttributes.Directory))
        {
            var directory = new DirectoryInfo(file.FullName);
            foreach (var subFile in directory.GetFileSystemInfos())
            {
                if (PassesFilter(new FileInfo(subFile.FullName)))
                    return true;
            }
        }

        return false;
    }
}
