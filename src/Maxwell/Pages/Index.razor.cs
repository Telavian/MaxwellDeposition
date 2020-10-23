using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Maxwell.Extensions;
using Maxwell.Pages.Common;
using Maxwell.Pages.Models;
using Microsoft.AspNetCore.Components;

namespace Maxwell.Pages
{
    public class IndexBase : ViewModelBase
    {
        #region Private Variables

        [Inject]
        private HttpClient _httpClient { get; set; }

        private bool _isInitialized = false;
        private Dictionary<int, DocumentPage> _documentLookup = new Dictionary<int, DocumentPage>();

        #endregion

        #region Public Properties

        #region SearchLocations

        private string _searchLocations = "";

        public string SearchLocations
        {
            get => _searchLocations;
            set => UpdateProperty(ref _searchLocations, value);
        }

        #endregion SearchLocations

        #region SearchResults

        private MarkupString _searchResults = new MarkupString();

        public MarkupString SearchResults
        {
            get => _searchResults;
            set => UpdateProperty(ref _searchResults, value);
        }

        #endregion SearchResults

        #region StartFilterWord

        private string _startFilterWord = "";

        public string StartFilterWord
        {
            get => _startFilterWord;
            set
            {
                UpdateProperty(ref _startFilterWord, value);
                CreateSearchResults();
            }
        }

        #endregion StartFilterWord

        #region EndFilterWord

        private string _endFilterWord = "";

        public string EndFilterWord
        {
            get => _endFilterWord;
            set
            {
                UpdateProperty(ref _endFilterWord, value);
                CreateSearchResults();
            }
        }

        #endregion EndFilterWord

        #region AllSearchMatches

        private SearchMatch[] _allSearchMatches = Array.Empty<SearchMatch>();

        public SearchMatch[] AllSearchMatches
        {
            get => _allSearchMatches;
            set => UpdateProperty(ref _allSearchMatches, value);
        }

        #endregion AllSearchMatches

        #region SearchCommand

        private Func<Task> _searchAsyncCommand = null;

        public Func<Task> SearchAsyncCommand
        {
            get
            {
                if (_searchAsyncCommand == null)
                {
                    _searchAsyncCommand = CreateEventCallbackAsyncCommand(
                        async () =>
                        {
                            await SearchAsync()
                                .AnyContext();
                        }, "Unable to search");
                }

                return _searchAsyncCommand;
            }
        }

        #endregion SearchCommand

        #endregion

        #region Private Methods

        private async Task SearchAsync()
        {
            await InitializeLookupAsync()
                .AnyContext();

            var locations = ParseSearchLocations(SearchLocations);
            AllSearchMatches = FindSearchMatches(locations);

            CreateSearchResults();
        }

        private async Task InitializeLookupAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                await InitializeDocumentLookupAsync()
                    .AnyContext();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to initialize document lookup", ex);
            }
        }

        private async Task InitializeDocumentLookupAsync()
        {
            var text = await _httpClient
                .GetStringAsync("/MaxwellDeposition/data/Maxwell.txt");

            ParseText(text);
        }

        private void ParseText(string text)
        {
            var lines = text.Split(Environment.NewLine);

            var currentDocument = (DocumentPage)null;            

            foreach (var line in lines)
            {
                if (line.StartsWith("Page "))
                {
                    currentDocument = InitializeDocumentPage(line);                    
                }
                else
                {
                    ParseLine(currentDocument, line);
                }
            }
        }

        private DocumentPage InitializeDocumentPage(string line)
        {
            var lineText = line
                .Replace("Page ", "")
                .Trim();

            var pageNumber = int.Parse(lineText);
            var document = new DocumentPage();
            document.PageNumber = pageNumber;

            _documentLookup.Add(document.PageNumber, document);
            return document;
        }

        private void ParseLine(DocumentPage page, string line)
        {
            var words = line.Split()
                .Where(x => x.Length > 0)
                .ToArray();

            if (words.Length == 0)
            {
                return;
            }

            var isDocumentLine = int.TryParse(words[0], out var lineNumber);

            if (isDocumentLine == false)
            {
                return;
            }

            var pageLine = new PageLine
            {
                LineNumber = lineNumber,
                Words = words
                    .Skip(1)
                    .Select(x => CleanWord(x))
                    .ToArray()
            };

            page.LineLookup.Add(pageLine.LineNumber, pageLine);
        }

        private string CleanWord(string word)
        {
            var characters = word
                .Where(x => char.IsPunctuation(x) == false)
                .ToArray();

            return new string(characters);
        }

        private SearchLocation[] ParseSearchLocations(string searchText)
        {
            var locations = searchText.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            return locations
                .SelectMany(x => ParseSearchLocation(x))
                .ToArray();
        }

        private SearchLocation[] ParseSearchLocation(string searchText)
        {
            var parts = searchText
                .Split(":", StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToArray();

            if (parts.Length != 2)
            {
                throw new Exception("Invalid search. Must have ':'");
            }

            var isValid = int.TryParse(parts[0], out var pageNumber);

            if (isValid == false)
            {
                throw new Exception("Page number is not valid");
            }

            return parts[1]
                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Select(x =>
                {
                    var isLineValid = int.TryParse(x, out var lineNumber);

                    if (isLineValid == false)
                    {
                        throw new Exception($"Line number '{x}' is not valid");
                    }

                    return new SearchLocation
                    {
                        Page = pageNumber,
                        Line = lineNumber
                    };
                })
                .ToArray();                       
        }

        private SearchMatch[] FindSearchMatches(SearchLocation[] locations)
        {
            if (locations == null || locations.Length == 0)
            {
                return Array.Empty<SearchMatch>();
            }

            var matches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var location in locations)
            {
                var line = GetSearchLine(location);

                foreach (var word in line.Words)
                {
                    if (matches.ContainsKey(word))
                    {
                        matches[word]++;
                    }
                    else
                    {
                        matches.Add(word, 1);
                    }
                }
            }

            return matches
                .Select(x =>
                {
                    var word = x.Key;
                    var level = (double)x.Value / locations.Length;

                    return new SearchMatch
                    {
                        Word = word,
                        MatchLevel = level
                    };
                })
                .ToArray();
        }

        private PageLine GetSearchLine(SearchLocation location)
        {
            try
            {
                return _documentLookup[location.Page].LineLookup[location.Line];
            }
            catch(Exception ex)
            {
                throw new Exception($"Unable to find search location {location.Page}:{location.Line}");
            }
        }

        private void CreateSearchResults()
        {
            var matches = (IEnumerable<SearchMatch>)AllSearchMatches;

            if (string.IsNullOrWhiteSpace(StartFilterWord) == false)
            {
                matches = matches
                    .Where(match => string.Compare(StartFilterWord, match.Word, StringComparison.OrdinalIgnoreCase) < 0);
            }

            if (string.IsNullOrWhiteSpace(EndFilterWord) == false)
            {
                matches = matches
                    .Where(match => string.Compare(EndFilterWord, match.Word, StringComparison.OrdinalIgnoreCase) > 0);
            }

            var currentMatches = matches
                .ToArray();

            if (matches == null || currentMatches.Length == 0)
            {
                SearchResults = (MarkupString)"No matches found";
                return;
            }

            var likelyMatches = currentMatches
                .Select(x => $"{x.Word}({x.MatchLevel:0%})")
                .OrderBy(x => x)
                .ToArray();
            
            var matchesText = string.Join("<br />", likelyMatches);
            SearchResults = (MarkupString)$"Results:<br />{matchesText}";
        }

        #endregion
    }
}
