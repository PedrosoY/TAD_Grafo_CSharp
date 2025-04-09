using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ProjetoGrafo
{
    class Grafo
    {

        private List<Vertice> vertices;

        public ReadOnlyCollection<Vertice> Vertices => vertices.AsReadOnly();

        public Grafo()
        {
            vertices = new List<Vertice>();
        }

        public void AdicionarVertice(string nomeVertice)
        {
            if (vertices.Any(v => v.NomeDoVertice == nomeVertice))
                return;
            vertices.Add(new Vertice(nomeVertice));
        }

        public Vertice InsertVertex(string nomeVertice)
        {
            Vertice vertice = new Vertice(nomeVertice);
            vertices.Add(vertice);
            return vertice;
        }

        public Aresta InsertEdge(Vertice verticeOrigem, Vertice verticeDestino, string nomeAresta = "")
        {
            if (verticeOrigem == null || verticeDestino == null)
            {
                Console.WriteLine("Vértices inválidos. A aresta não pode ser criada.");
                return null;
            }

            if (string.IsNullOrEmpty(nomeAresta))
                nomeAresta = $"{verticeOrigem.NomeDoVertice}_para_{verticeDestino.NomeDoVertice}";

            var novaAresta = new Aresta(verticeOrigem, verticeDestino, nomeAresta);

            verticeOrigem.AdicionarAresta(novaAresta);
            verticeDestino.AdicionarAresta(novaAresta);

            return novaAresta;
        }

        public void AdicionarAresta(string nomeVerticeOrigem, string nomeVerticeDestino)
        {
            Vertice verticeOrigem = vertices.FirstOrDefault(v => v.NomeDoVertice == nomeVerticeOrigem);
            Vertice verticeDestino = vertices.FirstOrDefault(v => v.NomeDoVertice == nomeVerticeDestino);

            if (verticeOrigem != null && verticeDestino != null)
            {
                InsertEdge(verticeOrigem, verticeDestino);
            }
            else
            {
                Console.WriteLine("Um dos vértices informados não foi encontrado.");
            }
        }

        public void CriarMultiplosVertices(int quantidadeDeVertices)
        {
            for (int i = 1; i <= quantidadeDeVertices; i++)
            {
                AdicionarVertice("Vertice" + i);
            }
        }

        public void CriarArestaPorIndice(int indiceOrigem, int indiceDestino)
        {
            AdicionarAresta("Vertice" + indiceOrigem, "Vertice" + indiceDestino);
        }

        public void ExibirListaDeAdjacencia()
        {
            foreach (var vertice in vertices)
            {
                Console.Write($"{vertice.NomeDoVertice} -> ");
                foreach (var aresta in vertice.Arestas)
                {
                    Vertice vizinho = aresta.ObterVizinhoDe(vertice);
                    Console.Write($"{vizinho.NomeDoVertice} ");
                }
                Console.WriteLine();
            }
        }

        public (Vertice, Vertice) EndVertices(Aresta arestaBuscada)
        {
            return (arestaBuscada.VerticeDeOrigem, arestaBuscada.VerticeDeDestino);
        }

        public Vertice Opposite(Vertice verticeDaAresta, Aresta arestaBase)
        {
            return arestaBase.ObterVerticeOposto(verticeDaAresta);
        }

        // Lê um arquivo de texto contendo a definição do grafo.
        public void LerArquivoDeTextoComGrafo(string caminhoArquivoTexto = "")
        {
            if (string.IsNullOrEmpty(caminhoArquivoTexto))
            {
                OpenFileDialog seletorDeArquivo = new OpenFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Filter = "Arquivos de Texto (*.txt)|*.txt|Todos os Arquivos (*.*)|*.*"
                };

                if (seletorDeArquivo.ShowDialog() == DialogResult.OK)
                {
                    caminhoArquivoTexto = seletorDeArquivo.FileName;
                }
                else
                {
                    Console.WriteLine("Nenhum arquivo foi selecionado.");
                    return;
                }
            }

            try
            {
                using (StreamReader leitorDeArquivo = new StreamReader(caminhoArquivoTexto))
                {
                    string linhaLida;
                    bool primeiraLinha = true;

                    while ((linhaLida = leitorDeArquivo.ReadLine()) != null)
                    {
                        if (primeiraLinha)
                        {
                            CriarMultiplosVertices(Convert.ToInt32(linhaLida));
                            primeiraLinha = false;
                        }
                        else
                        {
                            string[] valores = linhaLida.Split(' ');
                            int indiceOrigem = Convert.ToInt32(valores[0]);
                            int indiceDestino = Convert.ToInt32(valores[1]);
                            CriarArestaPorIndice(indiceOrigem, indiceDestino);
                        }
                    }
                }
            }
            catch (Exception excecao)
            {
                Console.WriteLine($"Erro ao ler o arquivo: {excecao.Message}");
            }
        }

        public bool AreAdjacent(Vertice verticeAnalisado, Vertice verticeComparar)
        {
            foreach (var aresta in verticeAnalisado.Arestas)
            {
                if (aresta.VerticeDeDestino == verticeComparar || aresta.VerticeDeOrigem == verticeComparar)
                    return true;
            }
            return false;
        }

        public void ReplaceEdge(Aresta arestaAnalisada, string novoNomeAresta)
        {
            if (arestaAnalisada != null)
                arestaAnalisada.ReplaceNomeAresta(novoNomeAresta);
        }

        public void ReplaceVertex(Vertice verticeAnalise, string novoNomeVertice)
        {
            if (verticeAnalise != null)
                verticeAnalise.ReplaceNome(novoNomeVertice);
        }

        public string RemoveVertex(Vertice verticeRemover)
        {
            string nomeVerticeRemovido = verticeRemover.NomeDoVertice;

            foreach (Aresta aresta in new List<Aresta>(verticeRemover.Arestas))
            {
                Vertice vizinho = aresta.ObterVizinhoDe(verticeRemover);
                vizinho.RemoverAresta(aresta);
            }
            verticeRemover.LimparArestas();
            vertices.Remove(verticeRemover);

            return nomeVerticeRemovido;
        }

        public string RemoverEdge(Aresta arestaRemover)
        {
            string nomeArestaRemovida = arestaRemover.NomeAresta;

            foreach (var vertice in vertices)
            {
                vertice.RemoverAresta(arestaRemover);
            }

            return nomeArestaRemovida;
        }

        public string EdgeValue(Aresta aresta)
        {
            return aresta.NomeAresta;
        }

        public string VertexValue(Vertice vertice)
        {
            return vertice.NomeDoVertice;
        }
    }

    class Vertice
    {
        private List<Aresta> arestas;

        public ReadOnlyCollection<Aresta> Arestas => arestas.AsReadOnly();

        public string NomeDoVertice { get; private set; }

        public Vertice(string nomeDoVertice)
        {
            NomeDoVertice = nomeDoVertice;
            arestas = new List<Aresta>();
        }

        internal void AdicionarAresta(Aresta aresta)
        {
            if (aresta != null && !arestas.Contains(aresta))
                arestas.Add(aresta);
        }

        public void RemoverAresta(Aresta aresta)
        {
            if (arestas.Contains(aresta))
                arestas.Remove(aresta);
        }

        public void LimparArestas()
        {
            arestas.Clear();
        }

        public void ReplaceNome(string novoNome)
        {
            NomeDoVertice = novoNome;
        }
    }

    class Aresta
    {
        public string NomeAresta { get; private set; }
        public Vertice VerticeDeOrigem { get; private set; }
        public Vertice VerticeDeDestino { get; private set; }

        public Aresta(Vertice origem, Vertice destino, string nomeAresta)
        {
            VerticeDeOrigem = origem;
            VerticeDeDestino = destino;
            NomeAresta = nomeAresta;
        }

        public void ReplaceNomeAresta(string novoNome)
        {
            NomeAresta = novoNome;
        }

        public Vertice ObterVizinhoDe(Vertice vertice)
        {
            if (vertice == VerticeDeOrigem)
                return VerticeDeDestino;
            if (vertice == VerticeDeDestino)
                return VerticeDeOrigem;

            throw new ArgumentException("O vértice informado não pertence a esta aresta.");
        }

        public Vertice ObterVerticeOposto(Vertice vertice)
        {
            if (vertice == VerticeDeOrigem)
                return VerticeDeDestino;
            if (vertice == VerticeDeDestino)
                return VerticeDeOrigem;

            Console.WriteLine($"A aresta não possui o vértice {vertice.NomeDoVertice}");
            return null;
        }
    }

    internal class ProgramaPrincipal
    {
        public static void EscreverLinha()
        {
            Console.WriteLine("================================================================");
        }

        public void TestarGrafo()
        {
            Grafo grafo = new Grafo();
            grafo.LerArquivoDeTextoComGrafo();

            EscreverLinha();
            grafo.ExibirListaDeAdjacencia();

            EscreverLinha();

            // endVertices(G, e): retorna referências para os dois vértices finais da aresta.
            if (grafo.Vertices.Count > 0 && grafo.Vertices[0].Arestas.Count > 0)
            {
                Aresta arestaExemplo = grafo.Vertices[0].Arestas[0];
                var (origem, destino) = grafo.EndVertices(arestaExemplo);
                Console.WriteLine($"Origem: {origem.NomeDoVertice}, Destino: {destino.NomeDoVertice}");

                EscreverLinha();

                // opposite(G, v, e): retorna o vértice oposto na aresta.
                Vertice verticeOposto = grafo.Opposite(origem, arestaExemplo);
                if (verticeOposto != null)
                    Console.WriteLine($"O vértice oposto a {origem.NomeDoVertice} na aresta {arestaExemplo.NomeAresta} é {verticeOposto.NomeDoVertice}");
                else
                    Console.WriteLine($"O vértice {origem.NomeDoVertice} não está na aresta");
            }

            EscreverLinha();

            // areAdjacent(G, v, w): verifica se os vértices são adjacentes.
            if (grafo.Vertices.Count > 1)
            {
                Vertice v1 = grafo.Vertices[0];
                Vertice v2 = grafo.Vertices[1];
                bool adjacentes = grafo.AreAdjacent(v1, v2);
                if (adjacentes)
                    Console.WriteLine($"O vértice {v1.NomeDoVertice} é adjacente ao vértice {v2.NomeDoVertice}");
                else
                    Console.WriteLine($"O vértice {v1.NomeDoVertice} NÃO é adjacente ao vértice {v2.NomeDoVertice}");
            }

            EscreverLinha();

            // replaceEdge(G, e, o): substitui o nome da aresta.
            if (grafo.Vertices.Count > 0 && grafo.Vertices[0].Arestas.Count > 0)
            {
                Aresta arestaExemplo = grafo.Vertices[0].Arestas[0];
                Console.WriteLine($"Nome da aresta antigo: {arestaExemplo.NomeAresta}");
                grafo.ReplaceEdge(arestaExemplo, "Mudei_Nome_Desta_Aresta_:)");
                Console.WriteLine($"Nome da aresta atualizado: {arestaExemplo.NomeAresta}");
            }

            EscreverLinha();

            // replaceVertex(G, v, o): substitui o nome do vértice.
            if (grafo.Vertices.Count > 0)
            {
                Vertice vertice = grafo.Vertices[0];
                Console.WriteLine($"Nome do vértice antigo: {vertice.NomeDoVertice}");
                grafo.ReplaceVertex(vertice, "Mudei_Nome_Deste_Vertice_:)");
                Console.WriteLine($"Nome do vértice atualizado: {vertice.NomeDoVertice}");
            }

            EscreverLinha();

            // insertVertex(G, o): insere um novo vértice e retorna sua referência.
            Vertice referenciaVertice = grafo.InsertVertex("AdicioneiEsseVerticeAqui");
            Console.WriteLine($"Referência do vértice: {referenciaVertice} e Nome: {referenciaVertice.NomeDoVertice}");

            EscreverLinha();

            // insertEdge(G, v, w, o): cria uma aresta e retorna sua referência.
            if (grafo.Vertices.Count > 5)
            {
                Aresta novaAresta = grafo.InsertEdge(grafo.Vertices[1], grafo.Vertices[5]);
                Console.WriteLine($"Aresta criada: {novaAresta.NomeAresta}");

                EscreverLinha();

                // removeVertex(G, v): remove um vértice e suas arestas.
                string nomeVerticeRemovido = grafo.RemoveVertex(grafo.Vertices[5]);
                Console.WriteLine($"Foi removido o vértice de nome {nomeVerticeRemovido}");

                EscreverLinha();

                // removeEdge(G, e): remove uma aresta do grafo.
                string nomeArestaRemovida = grafo.RemoverEdge(novaAresta);
                Console.WriteLine($"Foi removida a aresta de nome {nomeArestaRemovida}");
            }

            EscreverLinha();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Grafo grafo = new Grafo();
            bool executando = true;

            while (executando)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("======= MENU DO GRAFO =======");
                Console.ResetColor();
                Console.WriteLine("1 - Ler grafo de arquivo");
                Console.WriteLine("2 - Inserir vértice");
                Console.WriteLine("3 - Inserir aresta");
                Console.WriteLine("4 - Exibir lista de adjacência");
                Console.WriteLine("5 - Alterar nome do vértice");
                Console.WriteLine("6 - Alterar nome da aresta");
                Console.WriteLine("7 - Remover vértice");
                Console.WriteLine("8 - Remover aresta");
                Console.WriteLine("9 - Sair");
                Console.Write("Opção: ");
                string opcao = Console.ReadLine();
                Console.WriteLine();

                switch (opcao)
                {
                    case "1":
                        grafo.LerArquivoDeTextoComGrafo();
                        break;
                    case "2":
                        Console.Write("Digite o nome do vértice: ");
                        string nomeVertice = Console.ReadLine();
                        grafo.InsertVertex(nomeVertice);
                        break;
                    case "3":
                        Console.Write("Digite o nome do vértice de origem: ");
                        string origem = Console.ReadLine();
                        Console.Write("Digite o nome do vértice de destino: ");
                        string destino = Console.ReadLine();
                        grafo.AdicionarAresta(origem, destino);
                        break;
                    case "4":
                        // Não precisa de ação, pois a lista será exibida a seguir.
                        break;
                    case "5":
                        Console.Write("Digite o nome do vértice a ser alterado: ");
                        string nomeAntigo = Console.ReadLine();
                        Vertice verticeAlterar = null;
                        foreach (var v in grafo.Vertices)
                        {
                            if (v.NomeDoVertice.Equals(nomeAntigo, StringComparison.OrdinalIgnoreCase))
                            {
                                verticeAlterar = v;
                                break;
                            }
                        }
                        if (verticeAlterar != null)
                        {
                            Console.Write("Digite o novo nome do vértice: ");
                            string novoNomeVertice = Console.ReadLine();
                            grafo.ReplaceVertex(verticeAlterar, novoNomeVertice);
                        }
                        else
                        {
                            Console.WriteLine("Vértice não encontrado.");
                        }
                        break;
                    case "6":
                        Console.Write("Digite o nome da aresta a ser alterada (ex: origem_para_destino): ");
                        string nomeArestaAntigo = Console.ReadLine();
                        Aresta arestaAlterar = null;
                        foreach (var v in grafo.Vertices)
                        {
                            foreach (var a in v.Arestas)
                            {
                                if (a.NomeAresta.Equals(nomeArestaAntigo, StringComparison.OrdinalIgnoreCase))
                                {
                                    arestaAlterar = a;
                                    break;
                                }
                            }
                            if (arestaAlterar != null)
                                break;
                        }
                        if (arestaAlterar != null)
                        {
                            Console.Write("Digite o novo nome para a aresta: ");
                            string novoNomeAresta = Console.ReadLine();
                            grafo.ReplaceEdge(arestaAlterar, novoNomeAresta);
                        }
                        else
                        {
                            Console.WriteLine("Aresta não encontrada.");
                        }
                        break;
                    case "7":
                        Console.Write("Digite o nome do vértice a ser removido: ");
                        string nomeRemover = Console.ReadLine();
                        Vertice verticeRemover = null;
                        foreach (var v in grafo.Vertices)
                        {
                            if (v.NomeDoVertice.Equals(nomeRemover, StringComparison.OrdinalIgnoreCase))
                            {
                                verticeRemover = v;
                                break;
                            }
                        }
                        if (verticeRemover != null)
                        {
                            string nomeRemovido = grafo.RemoveVertex(verticeRemover);
                            Console.WriteLine($"Vértice '{nomeRemovido}' removido.");
                        }
                        else
                        {
                            Console.WriteLine("Vértice não encontrado.");
                        }
                        break;
                    case "8":
                        Console.Write("Digite o nome da aresta a ser removida (ex: origem_para_destino): ");
                        string nomeArestaRemover = Console.ReadLine();
                        Aresta arestaRemover = null;
                        foreach (var v in grafo.Vertices)
                        {
                            foreach (var a in v.Arestas)
                            {
                                if (a.NomeAresta.Equals(nomeArestaRemover, StringComparison.OrdinalIgnoreCase))
                                {
                                    arestaRemover = a;
                                    break;
                                }
                            }
                            if (arestaRemover != null)
                                break;
                        }
                        if (arestaRemover != null)
                        {
                            string nomeRemovidoAresta = grafo.RemoverEdge(arestaRemover);
                            Console.WriteLine($"Aresta '{nomeRemovidoAresta}' removida.");
                        }
                        else
                        {
                            Console.WriteLine("Aresta não encontrada.");
                        }
                        break;
                    case "9":
                        executando = false;
                        break;
                    default:
                        Console.WriteLine("Opção inválida.");
                        break;
                }

                // Exibe a lista de adjacência a cada interação.
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n=== Lista de Adjacência Atual ===");
                Console.ResetColor();
                grafo.ExibirListaDeAdjacencia();

                Console.WriteLine("\nPressione ENTER para continuar...");
                Console.ReadLine();
            }

            Console.WriteLine("Programa encerrado.");
        }

    }
}
