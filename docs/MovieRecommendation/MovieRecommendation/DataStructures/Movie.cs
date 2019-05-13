// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MovieRecommendation.DataStructures
{
    class Movie
    {
        public int movieId;

        public string movieTitle;

        private static string moviesdatasetpath = $"{Program.DatasetsLocation}/recommendation-movies.csv";

        public Lazy<List<Movie>> _movies = new Lazy<List<Movie>>(() => LoadMovieData(moviesdatasetpath));
        
        public Movie()
        {
        }

        public Movie Get(int id)
        {
            return _movies.Value.Single(m => m.movieId == id);
        }

        private static List<Movie> LoadMovieData(string moviesDatasetPath)
        {
            var result = new List<Movie>();
            var fileReader = File.OpenRead(moviesDatasetPath);
            var reader = new StreamReader(fileReader);
            try
            {
                var header = true;
                var index = 0;
                var line = string.Empty;
                while (!reader.EndOfStream)
                {
                    if (header)
                    {
                        line = reader.ReadLine();
                        header = false;
                    }
                    line = reader.ReadLine();
                    var fields = line.Split(',');
                    var movieId = int.Parse(fields[0].ToString().TrimStart(new char[] { '0' }));
                    var movieTitle = fields[1];
                    result.Add(new Movie() { movieId = movieId, movieTitle = movieTitle });
                    index++;
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }

            return result;
        }
    }
}
