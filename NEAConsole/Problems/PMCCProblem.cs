﻿namespace NEAConsole.Problems;

public class PMCCProblem(IList<(double x, double y)> data, double solution) : IProblem
{
    private readonly IList<(double x, double y)> data = data;
    private readonly double solution = solution;

    public void Display()
    {
        Console.WriteLine("Find the PMCC of the following data to 3 s.f.\n");

        foreach (var item in data)
            Console.WriteLine(item);

        Console.Write("\nr = ");
    }

    public IAnswer GetAnswer(IAnswer? oldAnswer = null, CancellationToken? ct = null)
    {
        var answer = InputMethods.ReadDouble(startingNum: (oldAnswer as DoubleAnswer)?.Answer, ct: ct);

        return new DoubleAnswer(answer);
    }

    public void DisplayAnswer(IAnswer answer)
        => Console.WriteLine((answer as DoubleAnswer ?? throw new InvalidOperationException()).Answer);

    public bool EvaluateAnswer(IAnswer answer)
        => (answer as DoubleAnswer ?? throw new InvalidOperationException()).Answer == Math.Round(solution, 3, MidpointRounding.AwayFromZero);

    public void Summarise(IAnswer? answer)
    {
        bool correct;
        try { correct = answer is not null && EvaluateAnswer(answer); }
        catch (InvalidOperationException) { correct = false; }
        if (correct)
        {
            Console.WriteLine("Correct!");
        }
        else
        {
            Console.WriteLine($"Incorrect. The correct answer was {solution}.");
        }
    }
}