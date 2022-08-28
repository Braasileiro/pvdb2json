using System.Text.Json;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;

using pvdb2json.Models;

namespace pvdb2json
{
    internal class Program
    {
        /*
         * Exclusions
         */
        static readonly string[] EXCLUDED_PERFORMER_ROLES = {
            "PSEUDO_DEFAULT",
            "PSEUDO_SAME",
        };


        /*
         * Entry
         */
        static void Main(string[] args)
        {
            // Root
            var command = new RootCommand("pv_db to json converter.");

            // Options
            var fileOption = new Option<FileInfo?>(
                name: "--input",
                description: "The pv_db.txt file to be converted to json.",
                isDefault: true,
                parseArgument: result =>
                {
                    if (result.Tokens.Count == 0)
                    {
                        result.ErrorMessage = "Please provide an input file.";

                        return null;
                    }

                    string? path = result.Tokens.Single().Value;

                    if (!File.Exists(path))
                    {
                        result.ErrorMessage = "File does not exist.";

                        return null;
                    }
                    else
                    {
                        return new FileInfo(path);
                    }
                }
            );

            var typeOption = new Option<int>(
                name: "--type",
                description: "The identifier type of converted entries.",
                getDefaultValue: () => 0
            );

            // Add Options
            command.AddOption(fileOption);
            command.AddOption(typeOption);

            // Handler
            command.SetHandler((file, type) =>
            {
                Parse(file!, type);
            }, fileOption, typeOption);

            // Invoke
            command.Invoke(args);
        }

        private static void Parse(FileInfo file, int type)
        {
            List<Song> songs = new();

            using (var reader = file.OpenText())
            {
                int id;
                string field;
                string value;
                string[] section1;
                string[] section2;

                int lastId = -1;
                Song? song = null;
                SongPerformer? performer = null;
                List<SongPerformer> performers = new();

                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    // Ignore comments and stray strings in the file
                    if (line.StartsWith('#') || !line.StartsWith("pv_"))
                    {
                        continue;
                    }

                    // Field and value sections
                    section1 = line.Split('.', 2);
                    section2 = section1[1].Split('=');

                    // Current sections
                    id = GetId(section1[0]);
                    field = section2[0];
                    value = section2[1].Trim();

                    if (lastId != id)
                    {
                        // Append song to the list
                        AppendSong(song, songs, performers);

                        // New objects
                        song = new Song(id, type);
                        performers = new();

                        // Parsing message
                        Console.WriteLine($"Parsing {section1[0]}...");
                    }

                    // Update LastId
                    lastId = id;

                    switch (field)
                    {
                        // Song Info
                        case "bpm":
                            song!.bpm = StrToInt(value);
                            break;

                        case "date":
                            song!.date = StrToInt(value);
                            break;

                        case "song_name_reading":
                            song!.reading = value;
                            break;

                        // Japanese    
                        case "song_name":
                            song!.jp.name = value;
                            break;

                        case "songinfo.arranger":
                            song!.jp.arranger = value;
                            break;

                        case "songinfo.illustrator":
                            song!.jp.illustrator = value;
                            break;

                        case "songinfo.lyrics":
                            song!.jp.lyrics = value;
                            break;

                        case "songinfo.music":
                            song!.jp.music = value;
                            break;

                        // English
                        case "song_name_en":
                            song!.en.name = value;
                            break;

                        case "songinfo_en.arranger":
                            song!.en.arranger = value;
                            break;

                        case "songinfo_en.illustrator":
                            song!.en.illustrator = value;
                            break;

                        case "songinfo_en.lyrics":
                            song!.en.lyrics = value;
                            break;

                        case "songinfo_en.music":
                            song!.en.music = value;
                            break;

                        // Performers
                        case var chara when Regex.IsMatch(chara, "performer.(.+).chara"):
                            if (!PerformerExists(value, performers))
                            {
                                // New Performer
                                performer = new SongPerformer
                                {
                                    chara = value
                                };
                            }

                            break;

                        case var role when Regex.IsMatch(role, "performer.(.+).type"):
                            if (performer != null)
                            {
                                if (ValidatePerformerRole(value))
                                {
                                    // Assign role
                                    performer.role = value;

                                    // Add current performer to list
                                    performers.Add(performer);
                                }

                                // Reinit
                                performer = null;
                            }

                            break;

                        default: continue;
                    }
                }

                // Append the last entry here because it is the end of the stream
                AppendSong(song, songs, performers);
            }

            // Write JSON
            var json = JsonSerializer.Serialize(
                songs,
                options: new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            var filename = $"{Path.GetFileNameWithoutExtension(file.Name)}.json";

            File.WriteAllText(filename, Regex.Unescape(json));

            Console.WriteLine($"Parsed to '{filename}'.");
        }


        /*
         * Utils
         */
        private static void AppendSong(Song? song, List<Song> songs, List<SongPerformer> performers)
        {
            if (song != null)
            {
                if (performers.Any())
                {
                    // Append performers array to object
                    song.performers = performers;
                }

                // Add the last object to the list
                songs.Add(song);
            }
        }

        private static int GetId(string section)
        {
            var result = section.Split("pv_")[1];

            return int.Parse(result.TrimStart('0'));
        }

        private static int StrToInt(string value)
        {
            if (int.TryParse(value, out int number))
            {
                return number;
            }

            return 0;
        }

        private static bool PerformerExists(string performer, List<SongPerformer> performers)
        {
            return performers.Any(o => o.chara == performer);
        }

        private static bool ValidatePerformerRole(string role)
        {
            return !EXCLUDED_PERFORMER_ROLES.Any(o => o == role);
        }
    }
}
