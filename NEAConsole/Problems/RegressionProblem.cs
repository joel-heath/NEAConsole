﻿namespace NEAConsole.Problems;
internal class RegressionProblem : IProblem
{
    private readonly IList<(double x, double y)> data;
    private readonly double[] solution;

    public void Display()
    {
        Console.WriteLine("Find the y on x least squares regression line of the following data in the form y = mx + c, giving m and c to 3 s.f.\n");

        foreach (var item in data)
            Console.WriteLine(item);

        Console.WriteLine();
        Console.WriteLine();
    }

    public IAnswer GetAnswer(IAnswer? oldAnswer = null, CancellationToken? ct = null)
    {
        var solutionNames = new string[] { "m", "c" };
        var answer = UIMethods.ReadValues(solutionNames, (o, u, d) => UIMethods.ReadDouble(startingNum:o, allowUpwardsEscape: u, allowDownwardsEscape: d), oldVals: (oldAnswer as ManyAnswer<double?>)?.Answer, ct: ct);

        return new ManyAnswer<double?>(answer);
    }

    public void DisplayAnswer(IAnswer answer)
        => Console.WriteLine((answer as ManyAnswer<double?> ?? throw new InvalidOperationException()).Answer);

    public bool EvaluateAnswer(IAnswer answer)
        => (answer as ManyAnswer<double?> ?? throw new InvalidOperationException()).Answer.Select((d, i) =>(d, i)).All((t) => t.d == Math.Round(solution[t.i], 3, MidpointRounding.AwayFromZero));

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

    public RegressionProblem(IList<(double x, double y)> data, (double m, double c) solution)
    {
        this.data = data;
        this.solution = new double[] { solution.m, solution.c };
    }
}