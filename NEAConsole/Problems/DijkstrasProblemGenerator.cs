﻿using NEAConsole.Graphs;
using NEAConsole.Matrices;
using System.Transactions;

namespace NEAConsole.Problems;
internal class DijkstrasProblemGenerator : IProblemGenerator
{
    public string DisplayText => "Dijkstra's";
    private readonly Random random;

    public IProblem Generate()
    {
        var dimension = random.Next(8, 11);
        Matrix tree = new(dimension);

        for (int i = 1; i < dimension; i++)
        {
            // pick one of the already connected vertices to connect this new one to
            var connector = random.Next(0, i);

            var weight = random.Next(1, 16);

            tree[connector, i] = weight;
            tree[i, connector] = weight;
        }

        // 5x^2 - 85x + 367   (see 2. Robert J. Prim's algorithm -- pg 10)
        var edgesToAdd = 5 * dimension * dimension - 85 * dimension + 367;

        for (int i = 0; i < edgesToAdd; i++)
        {
            int node1 = 0, node2 = 1;
            while (tree[node1, node2] != 0)
            {
                node1 = random.Next(0, dimension);
                node2 = SelectNodeToConnectTo(node1, dimension);
            }
            var weight = random.Next(1, 16);

            if (tree[node1, node2] != 0 || tree[node2, node1] != 0) throw new Exception("Did not successfully choose nodes that weren't already connected");

            tree[node1, node2] = weight;
            tree[node2, node1] = weight;
        }

        (var startingNode, var endingNode) = CreateGraph(tree);

        var distances = GraphUtils.Dijkstras(startingNode);

        return new DijkstrasProblem(tree, 'A', (char)('A' + tree.Rows - 1), distances[endingNode]);
    }

    public int SelectNodeToConnectTo(int connector, int dimension)
    {
        int connectend = connector;
        while (connector == connectend)
        {
            connectend = random.Next(0, dimension);
        }

        return connectend;
    }

    /// <summary>
    /// Creates a graph based on an adjacency matrix.
    /// </summary>
    /// <param name="adjacencyMatrix">Adjacency matrix representing the graph</param>
    /// <returns>The node represented by the first entry in the adjacency matrix, and the node represented by the last</returns>
    public static (Node start, Node finish) CreateGraph(Matrix adjacencyMatrix)
    {
        //      node id     node         (node id, connection weight) array
        Dictionary<int, (Node node, (int id, int weight)[] connections)> nodeLegend = new(); // could just be an array?
        for (int i = 0; i < adjacencyMatrix.Rows; i++)
        {
            (int, int)[] connections = new (int, int)[adjacencyMatrix.Columns];
            for (int j = 0; j < adjacencyMatrix.Columns; j++)
            {
                connections[j] = (j, (int)adjacencyMatrix[i, j]);
            }
            Node node = new(new());
            nodeLegend.Add(i, (node, connections));
        }

        foreach (var kvp in nodeLegend)
        {
            kvp.Value.node.Arcs = kvp.Value.connections.ToDictionary(c => (INode)nodeLegend[c.id].node, c => c.weight);
        }

        return (nodeLegend[0].node, nodeLegend[nodeLegend.Count - 1].node);
    }

    public DijkstrasProblemGenerator() : this(new Random()) { }
    public DijkstrasProblemGenerator(Random randomNumberGenerator) => random = randomNumberGenerator;
}