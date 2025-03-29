using System.Linq;
using Fastenshtein;

public class FilterManager(string filter, bool considerFolderContents, bool matchCase, bool typoTolerance, bool anyOrder)
{
    public void ApplyFilter(List<FileInfo> fileInfos)
    {
        fileInfos.RemoveAll(file => !PassesFilter(file));
    }

    private bool PassesFilter(FileInfo file)
    {
        string fileName = file.Name;
        string targetFilter = filter;

        if (!matchCase)
        {
            fileName = fileName.ToLower();
            targetFilter = targetFilter.ToLower();
        }
        if (fileName.Contains(targetFilter))
        {
            return true;
        }
        if (typoTolerance)
        {
            if (Levenshtein.Distance(targetFilter, fileName) <= 1)
            {
                return true;
            }
            string[] fileNameTokens = System.Text.RegularExpressions.Regex.Split(fileName, @"\W+");
            string[] filterTokens = targetFilter.Split(' ', StringSplitOptions.RemoveEmptyEntries);;
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
                if (targetFilter == tempFileName || Levenshtein.Distance(targetFilter, tempFileName) <= 1)
                {
                    return true;
                }
            }
            if (fileName.Length > targetFilter.Length)
            {
                var tempFileName = fileName.Substring(0, targetFilter.Length);
                if (targetFilter == tempFileName || Levenshtein.Distance(targetFilter, tempFileName) <= 1)
                {
                    return true;
                }
            }
        }
        else
        {
            if (anyOrder)
            {
                string[] filterTokens = anyOrder ? targetFilter.Split(' ') : new[] { targetFilter };
                bool allMatches = filterTokens.All(token => fileName.Contains(token));
                if (allMatches)
                {
                    return true;
                }
            }
        }

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
