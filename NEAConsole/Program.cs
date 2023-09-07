﻿using NEAConsole.Problems;
using System.Runtime.CompilerServices;
using System.Text.Json;

[assembly:InternalsVisibleTo("NEAConsoleTests")]

namespace NEAConsole;
internal class Program
{
    private const string USER_KNOWLEDGE_PATH = "UserKnowledge.json",
                         SAMPLE_KNOWLEDGE_PATH = "SampleKnowledge.json";
    private static DateTime lastBreak = DateTime.Now;
    private static TimeSpan studyLength = TimeSpan.FromMinutes(25);
    private static TimeSpan breakLength = TimeSpan.FromMinutes(5);

    /// <summary>
    /// If the user's knowledge tree is completely unknown, asks the user to update their knowledge tree, specifying which topics they know.
    /// </summary>
    /// <param name="knowledge">Skill representing knowledge tree.</param>
    /// <param name="quizTitle">Defaults to "random questions", will output "In order to use {quizTitle}, you must first enter the topics you know.</param>
    /// <returns>False if the user still doesn't know any topics, true if at least one topic is known.</returns>
    static bool TryUpdateKnowledge(Skill knowledge, string quizTitle = "use random questions")
    {
        // or a while? NO, client chose to have it this way
        if (knowledge.Children.All(s => !s.Known))
        {
            Console.WriteLine($"To {quizTitle} you must first enter the topics you know.");
            UIMethods.Wait(string.Empty);
            Console.Clear();
            UpdateKnowledge(knowledge, false);

            if (knowledge.Children.All(s => !s.Known))
            {
                Console.WriteLine($"You cannot {quizTitle} if you do not know any topics.");
                UIMethods.Wait(string.Empty);
                Console.Clear();
                return false;
            }
        }

        return true;
    }

    public static void UpdateKnowledge(Skill knowledge, bool catchEscape = true)
    {
        var oldSkills = knowledge.Children;
        knowledge.ResetKnowledge(SAMPLE_KNOWLEDGE_PATH);

        try { UIMethods.UpdateAllSkills(knowledge.Children); }
        catch (EscapeException)
        {
            knowledge.Children = oldSkills; // undo the resetting of knowledge
            Console.Clear();
            if (!catchEscape) throw new EscapeException();
        }

        File.WriteAllText(USER_KNOWLEDGE_PATH, JsonSerializer.Serialize(knowledge.Children));//, new JsonSerializerOptions { WriteIndented = true }));
    }

    static void SettingsMenu(Skill knowledge)
    {
        var options = new MenuOption[]
        {
            ("Update Knowledge", (k) => UpdateKnowledge(k, true)),
            ("Study Break Timer", (k) => {
                Console.Write("How many minutes do you want to study for? ");
                studyLength = TimeSpan.FromMinutes(UIMethods.ReadInt(startingNum:studyLength.Minutes));
                Console.Write("How many minutes should the break be? ");
                breakLength = TimeSpan.FromMinutes(UIMethods.ReadInt(startingNum:breakLength.Minutes));
            }),
#if DEBUG
            ("Clear Knowledge", (k) =>
            {
                File.WriteAllBytes(USER_KNOWLEDGE_PATH, File.ReadAllBytes(SAMPLE_KNOWLEDGE_PATH));
                k.ResetKnowledge(SAMPLE_KNOWLEDGE_PATH);
            })
#endif
        };

        Menu.ExecuteMenu(options, "Settings", knowledge);
        Console.Clear();
    }

    static void MathsMenu(Skill knowledge)
    {

    }

    static void MatricesMenu(Skill knowledge)
    {
        IProblemGenerator[] options = { new MatricesAdditionProblemGenerator(), new MatricesMultiplicationProblemGenerator(), new MatricesDeterminantsProblemGenerator(), new MatricesInversionProblemGenerator() }; // Hypothesis Testing

        Menu.ExecuteMenu(options, "Choose a subject to revise", knowledge);
        Console.Clear();
    }

    static void FMathsMenu(Skill knowledge)
    {
        IProblemGenerator[] options = { new SimplexProblemGenerator(), new PrimsProblemGenerator(), new DijkstrasProblemGenerator() }; // Hypothesis Testing

        Menu.ExecuteMenu(options.Select(o => o.ToMenuOption()).Prepend(("Matrices", MatricesMenu)), "Choose a subject to revise", knowledge);
        Console.Clear();
    }

    static void CSciMenu(Skill knowledge)
    {

    }

    static void TopicMenu(Skill knowledge)
    {
        var options = new MenuOption[]
        {
            ("Maths", MathsMenu),
            ("Further Maths", FMathsMenu),
            ("Computer Science", CSciMenu),
        };

        Menu.ExecuteMenu(options, "Select a topic to revise", knowledge);
        Console.Clear();
    }

    static void QuickfireQuestions(Skill knowledge)
    {
        if (!TryUpdateKnowledge(knowledge)) return;

        var gen = new RandomProblemGenerator();
        bool @continue = true;
        while (@continue)
        {
            var problem = gen.Generate(knowledge);
            problem.Display();

            try
            {
                var answer = problem.GetAnswer();
                problem.Summarise(answer);
                UIMethods.Wait();
                Console.Clear();
            }
            catch (EscapeException)
            {
                Console.Clear();
                @continue = false;
            }

            if (DateTime.Now - lastBreak > studyLength)
            {
                Console.WriteLine($"You've been studying for over {studyLength.Minutes} minutes! Do you want to take a break?");
                if (Menu.Affirm())
                {
                    CancellationTokenSource cts = new();
                    Console.WriteLine($"Take a rest for the next {breakLength.Minutes} minutes.");
                    var timeRemaining = breakLength;
                    var second = TimeSpan.FromSeconds(1);

                    var affirmation = Task.Run(() =>
                    {
                        try { UIMethods.Wait("Press any key to skip the break.", cts.Token); }
                        catch (KeyNotFoundException) { }
                        finally { cts.Cancel(); }
                    });
                    var timer = Task.Run(() => Exam.WriteTimer(timeRemaining));

                    while (!cts.Token.IsCancellationRequested && (timeRemaining.Seconds > 0 || timeRemaining > TimeSpan.Zero)) // do our actual calculation for whether time is up based on datetimes, more accurately
                    {
                        Thread.Sleep(second);
                        timeRemaining -= second;
                        timer = Task.Run(() => Exam.WriteTimer(timeRemaining));
                    }

                    cts.Cancel();

                    while (!affirmation.IsCompletedSuccessfully && !timer.IsCompletedSuccessfully) { }
                    affirmation.Dispose();
                    timer.Dispose();
                }
                lastBreak = DateTime.Now;
                Console.Clear();
            }
        }
    }

    static void MockExam(Skill knowledge)
    {
        TryUpdateKnowledge(knowledge, "start a mock exam");
        Skill? chosenKnowledge = Exam.CreateKnowledge(USER_KNOWLEDGE_PATH);
        if (chosenKnowledge is null) return;
        Exam exam = new(chosenKnowledge);

        exam.Begin();
    }

    static void Main()//string[] args)
    {
        if (!File.Exists(USER_KNOWLEDGE_PATH)) File.WriteAllBytes(USER_KNOWLEDGE_PATH, File.ReadAllBytes(SAMPLE_KNOWLEDGE_PATH));
        Skill knowledge = Skill.KnowledgeConstructor(USER_KNOWLEDGE_PATH);
        
        lastBreak = DateTime.Now;

        var options = new MenuOption[]
        {
            ("Mock Exam", MockExam),
            ("Quickfire Questions", QuickfireQuestions),
            ("Topic Select", TopicMenu),
            ("Settings", SettingsMenu),
        };

        Menu.ExecuteMenu(options, "Main Menu", knowledge);
        Console.Clear();
    }
}