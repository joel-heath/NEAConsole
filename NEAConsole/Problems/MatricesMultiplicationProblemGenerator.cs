﻿using NEAConsole.Matrices;

namespace NEAConsole.Problems;

public class MatricesMultiplicationProblemGenerator(IRandom randomNumberGenerator) : IProblemGenerator
{
    public string DisplayText => "Matrix Multiplication";
    public string SkillPath => "Matrices.Multiplication";
    private readonly IRandom random = randomNumberGenerator;

    public IProblem Generate(Skill knowledge)
    {
        (int rows, int cols) = (random.Next(1, 4), random.Next(1, 4));

        Matrix mat1 = new(rows, cols, Enumerable.Range(0, rows * cols).Select(n => (double)random.Next(-10, 10)));
        Matrix mat2 = new(cols, rows = random.Next(1, 4), Enumerable.Range(0, rows * cols).Select(n => (double)random.Next(-10, 10)));

        Matrix answer = mat1 * mat2;

        return new MatricesMultiplicationProblem(mat1, mat2, answer);
    }

    public MatricesMultiplicationProblemGenerator() : this(new Random()) { }
}