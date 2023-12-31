﻿using NEAConsole.Matrices;

namespace NEAConsole.Problems;

public class MatricesInversionProblem(Matrix mat, Matrix solution) : IProblem
{
    private readonly Matrix mat = mat;
    private readonly Matrix solution = solution;

    public void Display()
    {
        UIMethods.DrawMatrix(mat);

        var signSpacing = (mat.Rows - 1) / 2;

        Console.Write("-1");
        Console.CursorTop += signSpacing;
        Console.Write(" = ");
        Console.CursorTop -= signSpacing;
    }

    public IAnswer GetAnswer(IAnswer? oldAnswer = null, CancellationToken? ct = null)
    {
        var answer = InputMethods.InputMatrix(solution.Rows, solution.Columns, (oldAnswer as MatrixAnswer)?.Answer, ct);
        Console.WriteLine();

        return new MatrixAnswer(answer);
    }

    public void DisplayAnswer(IAnswer answer)
    {
        UIMethods.DrawMatrix((answer as MatrixAnswer ?? throw new InvalidOperationException()).Answer, false);
        Console.WriteLine();
    }

    public bool EvaluateAnswer(IAnswer answer)
        => (answer as MatrixAnswer ?? throw new InvalidOperationException()).Answer == solution;

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
            if (answer is null)
            {
                Console.CursorTop += mat.Columns;
                Console.WriteLine();
            }
            Console.WriteLine("Incorrect. The correct answer was: ");
            UIMethods.DrawMatrix(solution, false);
            Console.WriteLine();
        }
    }
}