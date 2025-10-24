
//Данилевский Тимур  Давидович P3208
using System.Text;
using System.Text.Json;


public record Movie(int MovieId, string Title, List<CastMember> Cast, List<CrewMember> Crew);

public record CastMember(
    int? Cast_Id,
    string? Character,
    string? Credit_id,
    int? Gender,
    int? Id,
    string? Name,
    int? Order
);

public record CrewMember(
    string? Credit_id,
    string? Department,
    int? Gender,
    int? Id,
    string? Job,
    string? Name
);


static List<Movie> LoadMovies(string csvPath)
{
    var movies = new List<Movie>();
    var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    using var sr = new StreamReader(csvPath, Encoding.UTF8);
    string? header = sr.ReadLine(); 

    while (!sr.EndOfStream)
    {
        var line = ReadCsvLine(sr);                  
        if (string.IsNullOrWhiteSpace(line)) continue;

        var cols = SplitCsv(line);
        if (cols.Count < 4) continue;

        int movieId = int.TryParse(cols[0], out var mid) ? mid : 0;
        string title = cols[1];

        var cast = JsonSerializer.Deserialize<List<CastMember>>(cols[2], jsonOpts) ?? new();
        var crew = JsonSerializer.Deserialize<List<CrewMember>>(cols[3], jsonOpts) ?? new();

        movies.Add(new Movie(movieId, title, cast, crew));
    }
    return movies;

    static string ReadCsvLine(StreamReader sr)
    {
       
        var sb = new StringBuilder();
        int quoteCount = 0;
        while (true)
        {
            var chunk = sr.ReadLine();
            if (chunk is null) break;
            if (sb.Length > 0) sb.Append('\n'); 
            sb.Append(chunk);
            quoteCount += chunk.Count(c => c == '"');
            if (quoteCount % 2 == 0) break; 
        }
        return sb.ToString();
    }

    static List<string> SplitCsv(string line)
    {
        var res = new List<string>(4);
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                res.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        res.Add(sb.ToString());
        
        for (int i = 0; i < res.Count; i++)
        {
            var s = res[i];
            if (s.Length >= 2 && s[0] == '"' && s[^1] == '"') res[i] = s[1..^1];
        }
        return res;
    }
}


static IEnumerable<(string A, string B)> UnorderedPairs(IEnumerable<string> names)
{
    var list = names.Distinct().OrderBy(x => x).ToList();
    for (int i = 0; i < list.Count; i++)
        for (int j = i + 1; j < list.Count; j++)
            yield return (list[i], list[j]);
}

static void PrintHeader(string title)
{
    Console.WriteLine();
    Console.WriteLine(new string('─', Math.Max(10, title.Length)));
    Console.WriteLine(title);
    Console.WriteLine(new string('─', Math.Max(10, title.Length)));
}


static void RunQueries(List<Movie> movies)
{
    // 1) Все фильмы Spielberg
    PrintHeader("Фильмы Steven Spielberg");
    var spielbergFilms = movies
        .Where(m => m.Crew.Any(c => c.Job == "Director" && c.Name == "Steven Spielberg"))
        .Select(m => m.Title);
    Console.WriteLine(string.Join(", ", spielbergFilms));

    // 2) Все персонажи Tom Hanks
    PrintHeader("Персонажи Tom Hanks");
    var hanksChars = movies
        .SelectMany(m => m.Cast.Where(c => c.Name == "Tom Hanks")
                               .Select(c => $"{m.Title}: {c.Character}"))
        .ToList();
    hanksChars.ForEach(Console.WriteLine);

    // 3) Топ-5 фильмов по размеру актерского состава
    PrintHeader("Топ-5 фильмов по числу актеров");
    movies.OrderByDescending(m => m.Cast.Count).ThenBy(m => m.Title).Take(5)
        .ToList()
        .ForEach(m => Console.WriteLine($"{m.Title} — {m.Cast.Count}"));

    // 4) Топ-10 самых востребованных актеров (по числу фильмов)
    PrintHeader("Топ-10 актеров по числу фильмов");
    movies.SelectMany(m => m.Cast.Select(c => c.Name!))
          .GroupBy(n => n)
          .Select(g => new { Actor = g.Key, Films = g.Count() })
          .OrderByDescending(x => x.Films).ThenBy(x => x.Actor).Take(10)
          .ToList()
          .ForEach(x => Console.WriteLine($"{x.Actor}: {x.Films}"));

    // 5) Все уникальные департаменты
    PrintHeader("Все департаменты (уникальные)");
    var departments = movies.SelectMany(m => m.Crew.Select(c => c.Department))
                            .Where(d => !string.IsNullOrWhiteSpace(d))
                            .Distinct()
                            .OrderBy(d => d);
    Console.WriteLine(string.Join(", ", departments));

    // 6) Фильмы, где Hans Zimmer — Original Music Composer
    PrintHeader("Фильмы с Hans Zimmer (Original Music Composer)");
    var zimmerFilms = movies.Where(m => m.Crew.Any(c => c.Name == "Hans Zimmer" && c.Job == "Original Music Composer"))
                            .Select(m => m.Title);
    Console.WriteLine(string.Join(", ", zimmerFilms));

    // 7) Словарь: MovieId -> Director
    PrintHeader("Словарь {MovieId -> Director}");
    var movieToDirector = movies.ToDictionary(
        m => m.MovieId,
        m => m.Crew.FirstOrDefault(c => c.Job == "Director")?.Name ?? "(No Director)");
    foreach (var kv in movieToDirector.Take(20)) 
        Console.WriteLine($"{kv.Key} -> {kv.Value}");

    // 8) Фильмы с Brad Pitt и George Clooney
    PrintHeader("Фильмы с Brad Pitt И George Clooney");
    var pittClooney = movies.Where(m =>
        m.Cast.Any(c => c.Name == "Brad Pitt") && m.Cast.Any(c => c.Name == "George Clooney"))
        .Select(m => m.Title);
    Console.WriteLine(string.Join(", ", pittClooney));

    // 9) Сколько людей в департаменте Camera (уникально по имени)
    PrintHeader("Всего людей, работавших в департаменте Camera (уникальные имена)");
    var cameraPeopleCount = movies.SelectMany(m => m.Crew.Where(c => c.Department == "Camera")
                                                         .Select(c => c.Name!))
                                  .Where(n => !string.IsNullOrWhiteSpace(n))
                                  .Distinct()
                                  .Count();
    Console.WriteLine(cameraPeopleCount);

    // 10) В Titanic были и в Crew, и в Cast
    PrintHeader("Titanic: люди и в Crew, и в Cast");
    var titanic = movies.FirstOrDefault(m => m.Title.Equals("Titanic", StringComparison.OrdinalIgnoreCase));
    if (titanic != null)
    {
        var castNames = titanic.Cast.Select(c => c.Name!).ToHashSet();
        var crewNames = titanic.Crew.Select(c => c.Name!).ToHashSet();
        var both = castNames.Intersect(crewNames).OrderBy(x => x);
        Console.WriteLine(string.Join(", ", both));
    }

    // 11) «Внутренний круг» Тарантино: топ-5 не актеров из Crew по числу совместных фильмов
    PrintHeader("Внутренний круг Quentin Tarantino (Crew, не актеры): топ-5");
    var tarantinoMovies = movies.Where(m => m.Crew.Any(c => c.Job == "Director" && c.Name == "Quentin Tarantino")).ToList();
    tarantinoMovies.SelectMany(m => m.Crew.Where(c => c.Name != "Quentin Tarantino"))
        .GroupBy(c => c.Name!)
        .Select(g => new { Person = g.Key, Films = g.Select(x => tarantinoMovies.First(m => m.Crew.Contains(x)).Title).Distinct().Count() })
        .OrderByDescending(x => x.Films).ThenBy(x => x.Person).Take(5)
        .ToList()
        .ForEach(x => Console.WriteLine($"{x.Person}: {x.Films}"));

    // 12) Экранные «дуэты»: 10 пар, чаще всего снимавшихся вместе
    PrintHeader("Топ-10 актерских дуэтов");
    movies.SelectMany(m => UnorderedPairs(m.Cast.Select(c => c.Name!).Where(n => !string.IsNullOrWhiteSpace(n))))
          .GroupBy(p => p)
          .Select(g => new { Pair = g.Key, Count = g.Count() })
          .OrderByDescending(x => x.Count)
          .ThenBy(x => x.Pair.A).ThenBy(x => x.Pair.B)
          .Take(10)
          .ToList()
          .ForEach(x => Console.WriteLine($"{x.Pair.A} + {x.Pair.B}: {x.Count}"));

    // 13) «Индекс разнообразия» карьеры: 5 членов Crew с макс. числом разных департаментов
    PrintHeader("Топ-5 по числу разных департаментов (Crew)");
    movies.SelectMany(m => m.Crew.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
          .GroupBy(c => c.Name!)
          .Select(g => new { Person = g.Key, DeptCount = g.Where(c => !string.IsNullOrWhiteSpace(c.Department))
                                                          .Select(c => c.Department!).Distinct().Count() })
          .OrderByDescending(x => x.DeptCount).ThenBy(x => x.Person).Take(5)
          .ToList()
          .ForEach(x => Console.WriteLine($"{x.Person}: {x.DeptCount}"));

    // 14) «Творческие трио»: человек = Director & Writer & Producer в одном фильме
    PrintHeader("Творческие трио (один человек: Director, Writer, Producer)");
    foreach (var m in movies)
    {
        var byPerson = m.Crew.Where(c => !string.IsNullOrWhiteSpace(c.Name))
                             .GroupBy(c => c.Name!);
        foreach (var g in byPerson)
        {
            var jobs = g.Select(c => c.Job ?? "").ToHashSet();
            bool isDirector = jobs.Contains("Director");
            bool isWriter = jobs.Contains("Writer") || jobs.Contains("Screenplay") || jobs.Contains("Story");
            bool isProducer = jobs.Contains("Producer") || jobs.Contains("Executive Producer");
            if (isDirector && isWriter && isProducer)
                Console.WriteLine($"{m.Title}: {g.Key}");
        }
    }

    // 15) «Два шага до Кевина Бейкона»
    PrintHeader("Два шага до Kevin Bacon");
    var kb = "Kevin Bacon";
    var kbFilms = movies.Where(m => m.Cast.Any(c => c.Name == kb)).ToList();
    var kbCo = kbFilms.SelectMany(m => m.Cast.Where(c => c.Name != kb).Select(c => c.Name!)).ToHashSet();

    var twoSteps = movies
        .Where(m => m.Cast.Any(c => kbCo.Contains(c.Name!)))
        .SelectMany(m => m.Cast.Select(c => c.Name!))
        .Where(n => n != kb && !kbCo.Contains(n))
        .Distinct()
        .OrderBy(n => n);
    Console.WriteLine(string.Join(", ", twoSteps));

    // 16) «Командная работа»: по режиссеру средний размер Cast и Crew
    PrintHeader("Средние размеры Cast и Crew по режиссерам");
    var directorMovies = movies.SelectMany(m => m.Crew.Where(c => c.Job == "Director")
                                                      .Select(c => new { Director = c.Name!, CastCount = m.Cast.Count, CrewCount = m.Crew.Count }));
    directorMovies.GroupBy(x => x.Director)
        .Select(g => new { Director = g.Key, AvgCast = g.Average(x => x.CastCount), AvgCrew = g.Average(x => x.CrewCount), Films = g.Count() })
        .OrderByDescending(x => x.Films).ThenBy(x => x.Director)
        .Take(20) // печатаем топ-20 по числу фильмов
        .ToList()
        .ForEach(x => Console.WriteLine($"{x.Director}: avg cast {x.AvgCast:F1}, avg crew {x.AvgCrew:F1} (films {x.Films})"));

    // 17) «Универсалы»: люди, которые и актеры, и в Crew; их самый частый департамент
    PrintHeader("Карьерный путь «универсалов»: доминирующий департамент");
    var actorNames = movies.SelectMany(m => m.Cast.Select(c => c.Name!)).ToHashSet();
    var crewByPerson = movies.SelectMany(m => m.Crew.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
                             .GroupBy(c => c.Name!);
    foreach (var g in crewByPerson.Where(g => actorNames.Contains(g.Key)).Take(30)) 
    {
        var topDept = g.Where(c => !string.IsNullOrWhiteSpace(c.Department))
                       .GroupBy(c => c.Department!)
                       .OrderByDescending(gg => gg.Count())
                       .Select(gg => gg.Key)
                       .FirstOrDefault() ?? "(unknown)";
        Console.WriteLine($"{g.Key}: {topDept}");
    }

    // 18) Пересечение «элитных клубов»: работали и со Scorsese, и с Nolan
    PrintHeader("Работали и со Scorsese, и с Nolan");
    HashSet<string> WorkedWith(string director) => movies
        .Where(m => m.Crew.Any(c => c.Job == "Director" && c.Name == director))
        .SelectMany(m => m.Cast.Select(c => c.Name!).Concat(m.Crew.Select(c => c.Name!)))
        .Where(n => !string.IsNullOrWhiteSpace(n) && n != director)
        .ToHashSet();

    var scSet = WorkedWith("Martin Scorsese");
    var noSet = WorkedWith("Christopher Nolan");
    Console.WriteLine(string.Join(", ", scSet.Intersect(noSet).OrderBy(x => x)));

    // 19) «Скрытое влияние»: департаменты по среднему размеру Cast
    PrintHeader("Рейтинг департаментов по среднему Cast в фильмах, где они задействованы");
    var deptAverages = movies
        .SelectMany(m => m.Crew.Select(c => (Dept: c.Department, Movie: m)))
        .Where(x => !string.IsNullOrWhiteSpace(x.Dept))
        .GroupBy(x => x.Dept!)
        .Select(g => new { Dept = g.Key, AvgCast = g.Select(x => x.Movie).Distinct().Average(m => m.Cast.Count) })
        .OrderByDescending(x => x.AvgCast);
    foreach (var x in deptAverages)
        Console.WriteLine($"{x.Dept}: {x.AvgCast:F2}");

    // 20) «Архетипы» ролей Johnny Depp
    PrintHeader("Архетипы персонажей Johnny Depp (по первому слову)");
    var jdRoles = movies.SelectMany(m => m.Cast.Where(c => c.Name == "Johnny Depp" && !string.IsNullOrWhiteSpace(c.Character))
                                               .Select(c => c.Character!));
    jdRoles.Select(ch => ch.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "(unknown)")
           .GroupBy(x => x)
           .Select(g => new { Archetype = g.Key, Count = g.Count() })
           .OrderByDescending(x => x.Count).ThenBy(x => x.Archetype)
           .ToList()
           .ForEach(x => Console.WriteLine($"{x.Archetype}: {x.Count}"));
}


var path = "tmdb_5000_credits.csv"; 
var movies = LoadMovies(path);
RunQueries(movies);
