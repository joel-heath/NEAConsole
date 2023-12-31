﻿using NEAConsole.Problems;

namespace NEAConsole;

public class Exam
{
    private int question;
    private readonly int questionCount, totalSeconds;
    private readonly RandomProblemGenerator gen;
    private readonly List<(IProblem problem, IAnswer? answer)> attempts;

    async public Task Begin(StudyTimer studyTimer)
    {
        var cts = new CancellationTokenSource();
        var timeRemaining = TimeSpan.FromSeconds(totalSeconds);
        var second = TimeSpan.FromSeconds(1);

        InputMethods.Wait("Press any key to begin the exam...");
        Console.Clear();

        var exam = Task.Run(() =>
        {
            try { UseExam(cts, studyTimer); }
            catch (KeyNotFoundException) { } // if user is typing but exam timer runs out, then can early exit this way
            catch (EscapeException) { return false; }
            return true;
        });
        WriteTimer(timeRemaining);
        var examTimer = Task.Delay(timeRemaining, cts.Token);
        var timeRemainingWriter = new Timer((_) => {
            timeRemaining -= second;            
            WriteTimer(timeRemaining);
        }, null, second, second);

        await Task.WhenAny(exam, examTimer);
        cts.Cancel();

        timeRemainingWriter.Dispose();
        await exam;
        if (!exam.Result) throw new EscapeException();
        exam.Dispose();
        examTimer.Dispose();

        Console.Clear();
        Console.WriteLine($"Exam complete.");

        InputMethods.Wait();
        Console.Clear();

        Review(studyTimer);
    }

    public static void WriteTimer(TimeSpan remaining)
    {
        int returnX = Console.CursorLeft, returnY = Console.CursorTop;
        lock (Console.Lock)
        {
            Console.SetCursorPosition(Console.WindowWidth - 8, 0);
            Console.Write($"{remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}");
            Console.SetCursorPosition(returnX, returnY);
        }
    }

    private void UseExam(CancellationTokenSource cts, StudyTimer timer)
    {
        var start = DateTime.Now;
        
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
                    var choice = Menu.ExamMenu(["<-", $"Question {question}/{questionCount}", "->"], question, cts.Token);
                    question += choice;
                }
                catch (EscapeException)
                {
                    var now = DateTime.Now;
                    Console.Clear();
                    Console.WriteLine("Are you sure you want to exit the exam without finishing?");
                    if (Menu.Affirm(cts.Token))
                    {
                        timer.TimeSinceLastBreak += now - start;
                        throw;
                    }
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

        timer.TimeSinceLastBreak += DateTime.Now - start;
        cts.Cancel();
    }

    private void Review(StudyTimer timer)
    {
        var start = DateTime.Now;

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

                var choice = Menu.ExamMenu(["<-", $"Question {question}/{questionCount}", "->"], question);
                question += choice;
            }
            catch (EscapeException)
            {
                var now = DateTime.Now;
                Console.Clear();
                Console.WriteLine("Are you sure you want to finish reviewing the exam?");
                if (Menu.Affirm())
                {
                    timer.TimeSinceLastBreak += now - start;
                    throw;
                }
            }

            if (question == attempts.Count + 1)
            {
                var now = DateTime.Now;
                Console.Clear();
                Console.WriteLine("Are you sure you want to finish reviewing the exam?");
                if (!Menu.Affirm()) question--;
                else timer.TimeSinceLastBreak += now - start;
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
                if (child.Children.Length == 0)
                {
                    InputMethods.UpdateKnownSkills(new Skill[] { child });
                }
                else
                {
                    InputMethods.UpdateKnownSkills(child.Children, new string[] { child.Name }); // but they may not want determinants but do want inversion.
                }
            }
        }
        
        return chosenKnowledge;
    }

    public Exam(Skill knowledge, int questions)
    {
        questionCount = questions;
        gen = new RandomProblemGenerator(knowledge);
        attempts = Enumerable.Range(0, questionCount).Select(i => ((IProblem problem, IAnswer? answer))(gen.Generate(), null)).ToList();
        question = 1;
        totalSeconds = 120 * questionCount; // multiply each problem by its weight, however there is no link between problem and problem generator (and hence skill path...)
    }
}