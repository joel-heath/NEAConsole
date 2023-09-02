﻿namespace NEAConsole.Problems;
internal class RandomProblemGenerator : IProblemGenerator
{
    // TWO OPTIONS:
    // 1. WHEN USING REFLECTION TO FIND ALL PROBLEM GENERATORS, Where pg.DisplayText != this.DisplayText
    // 2. Make a bogus skill path such that its never satisfied.
    public string DisplayText => "Random Questions";
    public string SkillPath => string.Empty;
    private readonly Random random;

    public IProblem Generate(Skill knowledge)
    {
        var generators = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                            .Where(t => t.GetInterfaces().Contains(typeof(IProblemGenerator)))
                            .Select(t => (IProblemGenerator)Activator.CreateInstance(t)!)
                            .Where(g => knowledge.Query(g.SkillPath, out _) && g.DisplayText != DisplayText).ToArray();

        return generators[random.Next(generators.Length)].Generate(knowledge);
    }

    public RandomProblemGenerator() : this(new Random()) { }
    public RandomProblemGenerator(Random randomNumberGenerator) => random = randomNumberGenerator;
}