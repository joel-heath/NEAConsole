﻿using NEAConsole.Problems;

namespace NEAConsole;
public class Exam
{
    private int question;
    private readonly int questionCount;
    private readonly RandomProblemGenerator gen;
    private readonly List<(IProblem problem, IAnswer? answer)> attempts;

    public void Begin()
    {
        var cts = new CancellationTokenSource();
        var examLength = 10;
        var timeRemaining = new TimeSpan(0, 0, examLength);
        var second = new TimeSpan(0, 0, 1);

        UIMethods.Wait("Press any key to begin the exam...");
        Console.Clear();

        var exam = Task.Run(() =>
        {
            try { UseExam(cts); }
            catch (KeyNotFoundException) {  } // if user is typing but exam timer runs out, then can early exit this way
        });
        Task.Run(() => WriteTimer(timeRemaining));

        while (!cts.Token.IsCancellationRequested && timeRemaining.Seconds > 0) // do our actual calculation for whether time is up based on datetimes, more accurately
        {
            Thread.Sleep(second);
            timeRemaining -= second;
            Task.Run(() => WriteTimer(timeRemaining));
        }

        cts.Cancel();

        while (!exam.IsCompletedSuccessfully) { }
        exam.Dispose();

        Console.Clear();
        Console.WriteLine($"Exam complete.");

        UIMethods.Wait();
        Console.Clear();

        Summarise();
    }

    private static void WriteTimer(TimeSpan remaining)
    {
        int returnX = Console.CursorLeft, returnY = Console.CursorTop;
        Console.SetCursorPosition(Console.WindowWidth - 8, 0);
        Console.Write($"{remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}");
        Console.SetCursorPosition(returnX, returnY);
    }

    private void UseExam(CancellationTokenSource cts)
    {
        while (question < attempts.Count + 1) // needs to become until time is up
        {
            Console.WriteLine('[' + new string('#', question) + new string(' ', questionCount - question) + ']'); // progress bar
            Console.WriteLine($"<- Question {question}/{questionCount} ->");
            var (p, a) = attempts[question - 1];
            p.Display();
            try
            {
                a = p.GetAnswer(a, cts.Token);
                attempts[question - 1] = (p, a);
                question++;
            }
            catch (EscapeException)
            {
                try
                {
                    var choice = Menu.ExamMenu(new string[] { "<-", $"Question {question}/{questionCount}", "->" }, question, cts.Token);
                    question += choice;
                }
                catch (EscapeException e)
                {
                    Console.Clear();
                    Console.WriteLine("Are you sure you want to exit the exam without finishing?");
                    if (Menu.Affirm(cts.Token)) throw e;
                }
            }

            Console.Clear();

            if (question == attempts.Count + 1)
            {
                Console.WriteLine("Are you sure you want to finish the exam early?");
                var unanswered = attempts.Select((a, i) => (a, i)).Where(a => a.a.answer is null);
                foreach (var att in unanswered)
                {
                    Console.WriteLine($"WARNING: Question {att.i + 1} is unanswered.");
                }
                if (!Menu.Affirm(cts.Token))
                {
                    question--;
                    Console.Clear();
                }
            }
        }

        cts.Cancel();
    }

    private void Summarise()
    {
        var question = 1;
        while (question < attempts.Count + 1) // needs to become until time is up
        {
            Console.WriteLine('[' + new string('#', question) + new string(' ', questionCount - question) + ']'); // progress bar
            Console.WriteLine($"<- Question {question}/{questionCount} ->");
            var (p, a) = attempts[question - 1];
            p.Display();
            try
            {
                if (a is not null) p.DisplayAnswer(a);
                else Console.WriteLine("You did not enter an answer.");
                p.Summarise(a);

                var choice = Menu.ExamMenu(new string[] { "<-", $"Question {question}/{questionCount}", "->" }, question);
                question += choice;
            }
            catch (EscapeException e)
            {
                Console.Clear();
                Console.WriteLine("Are you sure you want to finish reviewing the exam?");
                if (Menu.Affirm()) throw e;
            }

            if (question == attempts.Count + 1)
            {
                Console.Clear();
                Console.WriteLine("Are you sure you want to finish reviewing the exam?");
                if (!Menu.Affirm()) question--;
            }

            Console.Clear();
        }
    }

    public static Skill? CreateKnowledge(string knowledgePath)
    {
        var chosenKnowledge = Skill.KnowledgeConstructor(knowledgePath); // cheating way to make a deep copy of the user's knowledge object, just recreate from the file 

        foreach (var child in chosenKnowledge.Children)
        {
            if (child.Known) // because don't want to say do you want to be tested on matrices, they say no, then ask what about matrices > addition
            {
                UIMethods.UpdateKnownSkills(child.Children, new string[] { child.Name }); // but they may not want determinants but do want inversion.
            }
        }

        bool noneKnown = true;
        foreach (var child in chosenKnowledge.Children)
        {
            if (child.Children.Any(c => c.Known))
            {
                noneKnown = false;
                child.Known = true;
            }
        }

        if (noneKnown)
        {
            Console.WriteLine($"You cannot start a mock exam without any topics.");
            UIMethods.Wait(string.Empty);
            Console.Clear();
            return null;
        }

        return chosenKnowledge;
    }

    public Exam(Skill knowledge)
    {
        Console.Write("How many questions would you like in the mock exam? ");
        questionCount = UIMethods.ReadInt();

        gen = new RandomProblemGenerator();
        attempts = Enumerable.Range(0, questionCount).Select(i => ((IProblem problem, IAnswer? answer))(gen.Generate(knowledge), null)).ToList();
        question = 1;
    }
}