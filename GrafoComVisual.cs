using System;
using System.Drawing;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace ProjetoGrafo
{
    public class Grafo
    {
        private List<Vertice> listaVertices;
        public ReadOnlyCollection<Vertice> Vertices => listaVertices.AsReadOnly();
        public Grafo() { listaVertices = new List<Vertice>(); }
        public void InserirVertice(string nomeVertice)
        {
            if (listaVertices.Any(v => v.Nome.Equals(nomeVertice, StringComparison.OrdinalIgnoreCase)))
                return;
            listaVertices.Add(new Vertice(nomeVertice));
        }
        public Vertice InserirOuObterVertice(string nomeVertice)
        {
            Vertice verticeExistente = listaVertices.FirstOrDefault(v => v.Nome.Equals(nomeVertice, StringComparison.OrdinalIgnoreCase));
            if (verticeExistente != null)
                return verticeExistente;
            Vertice novoVertice = new Vertice(nomeVertice);
            listaVertices.Add(novoVertice);
            return novoVertice;
        }
        public Aresta InserirAresta(Vertice verticeOrigem, Vertice verticeDestino, string nomeAresta = "")
        {
            if (verticeOrigem == null || verticeDestino == null)
                return null;
            // Verifica se ja existe aresta entre os vertices (independente da ordem)
            bool existeAresta = verticeOrigem.Arestas.Any(a =>
                (a.VerticeOrigem == verticeOrigem && a.VerticeDestino == verticeDestino) ||
                (a.VerticeOrigem == verticeDestino && a.VerticeDestino == verticeOrigem));
            if (existeAresta)
            {
                MessageBox.Show("Aresta ja existe entre " + verticeOrigem.Nome + " e " + verticeDestino.Nome + ".", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            if (string.IsNullOrEmpty(nomeAresta))
                nomeAresta = verticeOrigem.Nome + "_para_" + verticeDestino.Nome;
            Aresta novaAresta = new Aresta(verticeOrigem, verticeDestino, nomeAresta);
            verticeOrigem.AdicionarAresta(novaAresta);
            verticeDestino.AdicionarAresta(novaAresta);
            return novaAresta;
        }
        public void AdicionarAresta(string nomeOrigem, string nomeDestino)
        {
            Vertice verticeOrigem = listaVertices.FirstOrDefault(v => v.Nome.Equals(nomeOrigem, StringComparison.OrdinalIgnoreCase));
            Vertice verticeDestino = listaVertices.FirstOrDefault(v => v.Nome.Equals(nomeDestino, StringComparison.OrdinalIgnoreCase));
            if (verticeOrigem != null && verticeDestino != null)
                InserirAresta(verticeOrigem, verticeDestino);
        }
        public void CriarMultiplosVertices(int quantidade, int offset)
        {
            for (int indice = 1; indice <= quantidade; indice++)
            {
                InserirVertice("Vertice" + (indice + offset));
            }
        }

        public void CriarArestaPorIndice(int indiceOrigem, int indiceDestino)
        {
            AdicionarAresta("Vertice" + indiceOrigem, "Vertice" + indiceDestino);
        }
        public void LerArquivo(string caminhoArquivo = "", bool reset = true)
        {
            if (string.IsNullOrEmpty(caminhoArquivo))
            {
                OpenFileDialog seletorArquivo = new OpenFileDialog
                {
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Filter = "Arquivos de Texto (*.txt)|*.txt|Todos os Arquivos (*.*)|*.*"
                };
                if (seletorArquivo.ShowDialog() == DialogResult.OK)
                    caminhoArquivo = seletorArquivo.FileName;
                else
                {
                    MessageBox.Show("Nenhum arquivo foi selecionado.");
                    return;
                }
            }

            int offset = 0;
            if (reset)
            {
                listaVertices.Clear();
            }
            else
            {
                offset = listaVertices.Count;
            }

            try
            {
                using (StreamReader leitorArquivo = new StreamReader(caminhoArquivo))
                {
                    string linha;
                    bool primeiraLinha = true;
                    while ((linha = leitorArquivo.ReadLine()) != null)
                    {
                        if (primeiraLinha)
                        {
                            int quantidadeVerticesArquivo = Convert.ToInt32(linha);
                            CriarMultiplosVertices(quantidadeVerticesArquivo, offset);
                            primeiraLinha = false;
                        }
                        else
                        {
                            string[] partes = linha.Split(' ');
                            int origem = Convert.ToInt32(partes[0]);
                            int destino = Convert.ToInt32(partes[1]);
                            CriarArestaPorIndice(origem + offset, destino + offset);
                        }
                    }
                }
            }
            catch (Exception erro)
            {
                MessageBox.Show("Erro ao ler o arquivo: " + erro.Message);
            }
        }

        public string RemoverVertice(Vertice vertice)
        {
            string nomeRemovido = vertice.Nome;
            foreach (Aresta aresta in new List<Aresta>(vertice.Arestas))
            {
                Vertice vizinho = aresta.ObterVizinho(vertice);
                vizinho.RemoverAresta(aresta);
            }
            vertice.LimparArestas();
            listaVertices.Remove(vertice);
            return nomeRemovido;
        }
        public string RemoverAresta(Aresta aresta)
        {
            string nomeArestaRemovida = aresta.NomeAresta;
            foreach (Vertice vertice in listaVertices)
                vertice.RemoverAresta(aresta);
            return nomeArestaRemovida;
        }
        public void AlterarVertice(Vertice vertice, string novoNome)
        {
            if (vertice != null)
                vertice.AtualizarNome(novoNome);
        }
        public (List<Vertice> caminho, int custo) Dijkstra(Vertice origem, Vertice destino)
        {
            Dictionary<Vertice, int> distancias = new Dictionary<Vertice, int>();
            Dictionary<Vertice, Vertice> anteriores = new Dictionary<Vertice, Vertice>();
            foreach (Vertice vertice in listaVertices)
            {
                distancias[vertice] = int.MaxValue;
                anteriores[vertice] = null;
            }
            if (!listaVertices.Contains(origem) || !listaVertices.Contains(destino))
                return (null, -1);
            distancias[origem] = 0;
            List<Vertice> naoVisitados = new List<Vertice>(listaVertices);
            while (naoVisitados.Any())
            {
                Vertice verticeAtual = naoVisitados.OrderBy(v => distancias[v]).First();
                naoVisitados.Remove(verticeAtual);
                if (verticeAtual == destino)
                    break;
                foreach (Aresta aresta in verticeAtual.Arestas)
                {
                    Vertice vizinho = aresta.ObterVizinho(verticeAtual);
                    if (!naoVisitados.Contains(vizinho))
                        continue;
                    int pesoAresta = 1;
                    int distanciaAlternativa = distancias[verticeAtual] == int.MaxValue ? int.MaxValue : distancias[verticeAtual] + pesoAresta;
                    if (distanciaAlternativa < distancias[vizinho])
                    {
                        distancias[vizinho] = distanciaAlternativa;
                        anteriores[vizinho] = verticeAtual;
                    }
                }
            }
            if (distancias[destino] == int.MaxValue)
                return (null, -1);
            List<Vertice> caminhoMinimo = new List<Vertice>();
            Vertice percorre = destino;
            while (percorre != null)
            {
                caminhoMinimo.Insert(0, percorre);
                percorre = anteriores[percorre];
            }
            return (caminhoMinimo, distancias[destino]);
        }
    }
    public class Vertice
    {
        private List<Aresta> listaArestas;
        public ReadOnlyCollection<Aresta> Arestas => listaArestas.AsReadOnly();
        public string Nome { get; private set; }
        public Vertice(string nome)
        {
            Nome = nome;
            listaArestas = new List<Aresta>();
        }
        internal void AdicionarAresta(Aresta aresta)
        {
            if (aresta != null && !listaArestas.Contains(aresta))
                listaArestas.Add(aresta);
        }
        public void RemoverAresta(Aresta aresta)
        {
            if (listaArestas.Contains(aresta))
                listaArestas.Remove(aresta);
        }
        public void LimparArestas()
        {
            listaArestas.Clear();
        }
        public void AtualizarNome(string novoNome)
        {
            Nome = novoNome;
        }
        public override string ToString()
        {
            return Nome;
        }
    }
    public class Aresta
    {
        public string NomeAresta { get; private set; }
        public Vertice VerticeOrigem { get; private set; }
        public Vertice VerticeDestino { get; private set; }
        public Aresta(Vertice origem, Vertice destino, string nomeAresta)
        {
            VerticeOrigem = origem;
            VerticeDestino = destino;
            NomeAresta = nomeAresta;
        }
        public void AtualizarNome(string novoNome)
        {
            NomeAresta = novoNome;
        }
        public Vertice ObterVizinho(Vertice vertice)
        {
            if (vertice == VerticeOrigem)
                return VerticeDestino;
            if (vertice == VerticeDestino)
                return VerticeOrigem;
            throw new ArgumentException("O vertice informado nao pertence a esta aresta.");
        }
    }
    public class PainelGrafo : Panel
    {
        private Grafo grafo;
        private Dictionary<Vertice, PointF> posicaoVertices;
        private float zoomAtual;
        private PointF deslocamentoPan;
        public PainelGrafo(Grafo grafoInstancia)
        {
            grafo = grafoInstancia;
            DoubleBuffered = true;
            posicaoVertices = new Dictionary<Vertice, PointF>();
            zoomAtual = 1.0f;
            deslocamentoPan = new PointF(0, 0);
            this.MouseWheel += PainelGrafo_MouseWheel;
            this.MouseDown += PainelGrafo_MouseDown;
            this.MouseMove += PainelGrafo_MouseMove;
            this.MouseUp += PainelGrafo_MouseUp;
        }
        public void ResetView()
        {
            zoomAtual = 1.0f;
            deslocamentoPan = new PointF(0, 0);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics grafico = e.Graphics;
            grafico.TranslateTransform(deslocamentoPan.X, deslocamentoPan.Y);
            grafico.ScaleTransform(zoomAtual, zoomAtual);
            grafico.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Dictionary<Vertice, float> mapaDiametroVertices = new Dictionary<Vertice, float>();
            Font fonteRotulo = new Font("Segoe UI", 9, FontStyle.Bold);
            int margem = 10;
            foreach (Vertice vertice in grafo.Vertices)
            {
                // Calculo do Diâmetro para Cada Vértice
                SizeF tamanhoTexto = grafico.MeasureString(vertice.Nome, fonteRotulo);
                float diametroCalculado = Math.Max(tamanhoTexto.Width, tamanhoTexto.Height) + margem;
                mapaDiametroVertices[vertice] = diametroCalculado;
            }
            float diametroMaximo = mapaDiametroVertices.Values.DefaultIfEmpty(40).Max();
            posicaoVertices.Clear();
            int totalVertices = grafo.Vertices.Count;
            if (totalVertices == 1)
                posicaoVertices[grafo.Vertices[0]] = new PointF(ClientSize.Width / (2 * zoomAtual), ClientSize.Height / (2 * zoomAtual));
            else if (totalVertices > 1)
            {
                float espacamentoExtra = 20;
                // formula geometrica de um polígono regular inscrito em um circulo
                float raioCirculo = (diametroMaximo + espacamentoExtra) / (2 * (float)Math.Sin(Math.PI / totalVertices));
                PointF centroVirtual = new PointF(ClientSize.Width / (2 * zoomAtual), ClientSize.Height / (2 * zoomAtual));
                for (int i = 0; i < totalVertices; i++)
                {
                    // Distribuição Angular
                    float angulo = 2 * (float)Math.PI * i / totalVertices;
                    float x = centroVirtual.X + raioCirculo * (float)Math.Cos(angulo);
                    float y = centroVirtual.Y + raioCirculo * (float)Math.Sin(angulo);
                    posicaoVertices[grafo.Vertices[i]] = new PointF(x, y);
                }
            }
            foreach (Vertice vertice in grafo.Vertices)
            {
                foreach (Aresta aresta in vertice.Arestas)
                {
                    Vertice origem = aresta.VerticeOrigem;
                    Vertice destino = aresta.VerticeDestino;
                    if (posicaoVertices.ContainsKey(origem) && posicaoVertices.ContainsKey(destino))
                        using (Pen pincelLinha = new Pen(Color.LightGray, 2f))
                            grafico.DrawLine(pincelLinha, posicaoVertices[origem], posicaoVertices[destino]);
                }
            }
            foreach (Vertice vertice in grafo.Vertices)
            {
                if (posicaoVertices.ContainsKey(vertice))
                {
                    // Calculo para Desenhar Cada Vertice
                    PointF centro = posicaoVertices[vertice];
                    float diametro = mapaDiametroVertices[vertice];
                    float raio = diametro / 2;
                    RectangleF retanguloVertice = new RectangleF(centro.X - raio, centro.Y - raio, diametro, diametro);
                    using (SolidBrush pincelPreenchimento = new SolidBrush(Color.White))
                        grafico.FillEllipse(pincelPreenchimento, retanguloVertice);
                    using (Pen pincelBorda = new Pen(Color.Black, 2f))
                        grafico.DrawEllipse(pincelBorda, retanguloVertice);
                    StringFormat formatoTexto = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    grafico.DrawString(vertice.Nome, fonteRotulo, Brushes.Black, retanguloVertice, formatoTexto);
                }
            }
        }
        private void PainelGrafo_MouseWheel(object sender, MouseEventArgs e)
        {
            float incrementoZoom = e.Delta > 0 ? 1.1f : 0.9f;
            zoomAtual *= incrementoZoom;
            Invalidate();
        }
        private bool arrastando;
        private Point posicaoUltimoMouse;
        private void PainelGrafo_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                arrastando = true;
                posicaoUltimoMouse = e.Location;
            }
        }
        private void PainelGrafo_MouseMove(object sender, MouseEventArgs e)
        {
            if (arrastando)
            {
                int deltaX = e.X - posicaoUltimoMouse.X;
                int deltaY = e.Y - posicaoUltimoMouse.Y;
                deslocamentoPan.X += deltaX;
                deslocamentoPan.Y += deltaY;
                posicaoUltimoMouse = e.Location;
                Invalidate();
            }
        }
        private void PainelGrafo_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                arrastando = false;
        }
    }
    public class FormInserirAresta : Form
    {
        public ComboBox cmbOrigem { get; private set; }
        public ComboBox cmbDestino { get; private set; }
        private Button btnOk;
        private Button btnCancelar;
        private List<Vertice> listaVertices;

        public FormInserirAresta(List<Vertice> listaVertices)
        {
            this.listaVertices = listaVertices;

            Text = "Inserir Aresta";
            Width = 320;
            Height = 180;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label lblOrigem = new Label
            {
                Text = "Vértice de Origem:",
                Left = 10,
                Top = 20,
                AutoSize = true
            };
            cmbOrigem = new ComboBox
            {
                Left = 150,
                Top = 15,
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label lblDestino = new Label
            {
                Text = "Vértice de Destino:",
                Left = 10,
                Top = 60,
                AutoSize = true
            };
            cmbDestino = new ComboBox
            {
                Left = 150,
                Top = 55,
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            foreach (var vertice in listaVertices)
            {
                cmbOrigem.Items.Add(vertice);
            }
            if (cmbOrigem.Items.Count > 0)
            {
                cmbOrigem.SelectedIndex = 0;
            }
            cmbOrigem.SelectedIndexChanged += cmbOrigem_SelectedIndexChanged;

            AtualizarDestino();

            btnOk = new Button
            {
                Text = "OK",
                Left = 70,
                Width = 80,
                Top = 100,
                DialogResult = DialogResult.OK
            };
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Left = 160,
                Width = 80,
                Top = 100,
                DialogResult = DialogResult.Cancel
            };
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancelar;

            Controls.Add(lblOrigem);
            Controls.Add(cmbOrigem);
            Controls.Add(lblDestino);
            Controls.Add(cmbDestino);
            Controls.Add(btnOk);
            Controls.Add(btnCancelar);
        }

        private void cmbOrigem_SelectedIndexChanged(object sender, EventArgs e)
        {
            AtualizarDestino();
        }

        private void AtualizarDestino()
        {
            cmbDestino.Items.Clear();
            Vertice verticeSelecionado = cmbOrigem.SelectedItem as Vertice;
            foreach (var vertice in listaVertices)
            {
                if (!vertice.Equals(verticeSelecionado))
                {
                    cmbDestino.Items.Add(vertice);
                }
            }
            if (cmbDestino.Items.Count > 0)
                cmbDestino.SelectedIndex = 0;
        }
    }
    public class FormAlterarVertice : Form
    {
        public ComboBox cmbVertice { get; private set; }
        public TextBox txtNovoNome { get; private set; }
        private Button btnOk;
        private Button btnCancelar;
        public FormAlterarVertice(List<Vertice> listaVertices)
        {
            Text = "Alterar Vertice";
            Width = 320;
            Height = 220;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Label lblSelecionar = new Label
            {
                Text = "Selecione o Vertice:",
                Left = 10,
                Top = 20,
                AutoSize = true
            };
            cmbVertice = new ComboBox
            {
                Left = 150,
                Top = 15,
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var vertice in listaVertices)
                cmbVertice.Items.Add(vertice);
            if (cmbVertice.Items.Count > 0)
                cmbVertice.SelectedIndex = 0;
            Label lblNovoNome = new Label
            {
                Text = "Novo Nome:",
                Left = 10,
                Top = 70,
                AutoSize = true
            };
            txtNovoNome = new TextBox
            {
                Left = 150,
                Top = 65,
                Width = 140
            };
            btnOk = new Button
            {
                Text = "OK",
                Left = 70,
                Width = 80,
                Top = 120,
                DialogResult = DialogResult.OK
            };
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Left = 160,
                Width = 80,
                Top = 120,
                DialogResult = DialogResult.Cancel
            };
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancelar;
            Controls.Add(lblSelecionar);
            Controls.Add(cmbVertice);
            Controls.Add(lblNovoNome);
            Controls.Add(txtNovoNome);
            Controls.Add(btnOk);
            Controls.Add(btnCancelar);
        }
    }
    public class FormRemoverVertice : Form
    {
        public ComboBox cmbVertice { get; private set; }
        private Button btnOk;
        private Button btnCancelar;
        public FormRemoverVertice(List<Vertice> listaVertices)
        {
            Text = "Remover Vertice";
            Width = 320;
            Height = 180;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Label lblVertice = new Label
            {
                Text = "Selecione o Vertice:",
                Left = 10,
                Top = 20,
                AutoSize = true
            };
            cmbVertice = new ComboBox
            {
                Left = 150,
                Top = 15,
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var vertice in listaVertices)
                cmbVertice.Items.Add(vertice);
            if (cmbVertice.Items.Count > 0)
                cmbVertice.SelectedIndex = 0;
            btnOk = new Button
            {
                Text = "OK",
                Left = 70,
                Width = 80,
                Top = 80,
                DialogResult = DialogResult.OK
            };
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Left = 160,
                Width = 80,
                Top = 80,
                DialogResult = DialogResult.Cancel
            };
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancelar;
            Controls.Add(lblVertice);
            Controls.Add(cmbVertice);
            Controls.Add(btnOk);
            Controls.Add(btnCancelar);
        }
    }
    public class FormRemoverAresta : Form
    {
        public ComboBox cmbOrigem { get; private set; }
        public ComboBox cmbDestino { get; private set; }
        private Button btnOk;
        private Button btnCancelar;
        public FormRemoverAresta(List<Vertice> listaVertices)
        {
            Text = "Remover Aresta";
            Width = 320;
            Height = 180;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Label lblOrigem = new Label
            {
                Text = "Vertice de Origem:",
                Left = 10,
                Top = 20,
                AutoSize = true
            };
            cmbOrigem = new ComboBox
            {
                Left = 150,
                Top = 15,
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Label lblDestino = new Label
            {
                Text = "Vertice de Destino:",
                Left = 10,
                Top = 60,
                AutoSize = true
            };
            cmbDestino = new ComboBox
            {
                Left = 150,
                Top = 55,
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // Preenche somente o cmbOrigem com todos os vertices
            foreach (var vertice in listaVertices)
            {
                cmbOrigem.Items.Add(vertice);
            }
            if (cmbOrigem.Items.Count > 0)
                cmbOrigem.SelectedIndex = 0;
            // Associa o evento que atualiza o cmbDestino
            cmbOrigem.SelectedIndexChanged += cmbOrigem_SelectedIndexChanged;
            btnOk = new Button
            {
                Text = "OK",
                Left = 70,
                Width = 80,
                Top = 100,
                DialogResult = DialogResult.OK
            };
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Left = 160,
                Width = 80,
                Top = 100,
                DialogResult = DialogResult.Cancel
            };
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancelar;
            Controls.Add(lblOrigem);
            Controls.Add(cmbOrigem);
            Controls.Add(lblDestino);
            Controls.Add(cmbDestino);
            Controls.Add(btnOk);
            Controls.Add(btnCancelar);
            // Atualiza o cmbDestino com base no valor inicial do cmbOrigem
            AtualizarDestino();
        }
        private void cmbOrigem_SelectedIndexChanged(object sender, EventArgs e)
        {
            AtualizarDestino();
        }
        private void AtualizarDestino()
        {
            cmbDestino.Items.Clear();
            Vertice verticeSelecionado = cmbOrigem.SelectedItem as Vertice;
            if (verticeSelecionado != null)
            {
                HashSet<Vertice> verticesConectados = new HashSet<Vertice>();
                foreach (Aresta aresta in verticeSelecionado.Arestas)
                {
                    try
                    {
                        Vertice vizinho = aresta.ObterVizinho(verticeSelecionado);
                        if (verticesConectados.Add(vizinho))
                        {
                            cmbDestino.Items.Add(vizinho);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao obter vizinho: " + ex.Message);
                    }
                }
                if (cmbDestino.Items.Count > 0)
                    cmbDestino.SelectedIndex = 0;
            }
        }
    }
    public class FormDijkstra : Form
    {
        public ComboBox cmbOrigem { get; private set; }
        public ComboBox cmbDestino { get; private set; }
        private Button btnOk;
        private Button btnCancelar;
        public FormDijkstra(List<Vertice> listaVertices)
        {
            Text = "Dijkstra";
            Width = 320;
            Height = 180;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Label lblOrigem = new Label
            {
                Text = "Vertice de Origem:",
                Left = 10,
                Top = 20,
                AutoSize = true
            };
            cmbOrigem = new ComboBox
            {
                Left = 150,
                Top = 15,
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Label lblDestino = new Label
            {
                Text = "Vertice de Destino:",
                Left = 10,
                Top = 60,
                AutoSize = true
            };
            cmbDestino = new ComboBox
            {
                Left = 150,
                Top = 55,
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var vertice in listaVertices)
            {
                cmbOrigem.Items.Add(vertice);
                cmbDestino.Items.Add(vertice);
            }
            if (cmbOrigem.Items.Count > 0)
                cmbOrigem.SelectedIndex = 0;
            if (cmbDestino.Items.Count > 0)
                cmbDestino.SelectedIndex = 0;
            btnOk = new Button
            {
                Text = "OK",
                Left = 70,
                Width = 80,
                Top = 100,
                DialogResult = DialogResult.OK
            };
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Left = 160,
                Width = 80,
                Top = 100,
                DialogResult = DialogResult.Cancel
            };
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancelar;
            Controls.Add(lblOrigem);
            Controls.Add(cmbOrigem);
            Controls.Add(lblDestino);
            Controls.Add(cmbDestino);
            Controls.Add(btnOk);
            Controls.Add(btnCancelar);
        }
    }
    public class FrmPrincipal : Form
    {
        private Grafo grafo;
        private TextBox txtAdjacencia;
        private SplitContainer divisor;
        private Panel painelAcoes;
        private PainelGrafo painelGrafo;
        public FrmPrincipal()
        {
            Text = "Projeto Grafo";
            Width = 1000;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(45, 45, 48);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            grafo = new Grafo();
            txtAdjacencia = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            painelGrafo = new PainelGrafo(grafo) { Dock = DockStyle.Fill };
            painelAcoes = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(28, 28, 28)
            };
            Button btnLerArquivo = CriarBotao("Ler Arquivo", 10, BtnLerArquivo_Click);
            Button btnInserirVertice = CriarBotao("Inserir Vertice", 120, BtnInserirVertice_Click);
            Button btnInserirAresta = CriarBotao("Inserir Aresta", 230, BtnInserirAresta_Click);
            Button btnAlterarVertice = CriarBotao("Alterar Vertice", 340, BtnAlterarVertice_Click);
            Button btnRemoverVertice = CriarBotao("Remover Vertice", 450, BtnRemoverVertice_Click);
            Button btnRemoverAresta = CriarBotao("Remover Aresta", 560, BtnRemoverAresta_Click);
            Button btnDijkstra = CriarBotao("Dijkstra", 670, BtnDijkstra_Click);
            Button btnExibirAdjacencia = CriarBotao("Exibir Adjacencia", 780, BtnExibirAdjacencia_Click);
            painelAcoes.Controls.AddRange(new Control[] { btnLerArquivo, btnInserirVertice, btnInserirAresta, btnAlterarVertice, btnRemoverVertice, btnRemoverAresta, btnDijkstra, btnExibirAdjacencia });
            divisor = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            divisor.SplitterDistance = Height / 2;
            divisor.Panel1.Controls.Add(txtAdjacencia);
            divisor.Panel2.Controls.Add(painelGrafo);
            Controls.Add(divisor);
            Controls.Add(painelAcoes);
        }
        private Button CriarBotao(string textoBotao, int posicaoEsquerda, EventHandler acaoClick)
        {
            Button botao = new Button
            {
                Text = textoBotao,
                Left = posicaoEsquerda,
                Top = 10,
                Width = 100,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.White
            };
            botao.FlatAppearance.BorderSize = 0;
            botao.Click += acaoClick;
            return botao;
        }
        private void BtnLerArquivo_Click(object sender, EventArgs e)
        {
            DialogResult acao = MessageBox.Show(
                "Deseja sobrescrever o grafo atual ou adicionar os dados do novo arquivo?\n" +
                "Clique 'Sim' para sobrescrever e 'Não' para adicionar.",
                "Selecione a Ação",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (acao == DialogResult.Cancel)
            {
                return;
            }
            else if (acao == DialogResult.Yes)
            {
                grafo.LerArquivo(reset: true);
            }
            else if (acao == DialogResult.No)
            {
                grafo.LerArquivo(reset: false);
            }

            AtualizarAdjacencia();
            painelGrafo.Invalidate();
        }



        private void BtnInserirVertice_Click(object sender, EventArgs e)
        {
            string nomeVertice = Interaction.InputBox("Digite o nome do vértice:", "Inserir Vértice", "Vertice");
            if (!string.IsNullOrEmpty(nomeVertice))
            {
                if (grafo.Vertices.Any(v => v.Nome.Equals(nomeVertice, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Já existe um vértice com esse nome.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                grafo.InserirVertice(nomeVertice);
            }
            AtualizarAdjacencia();
            painelGrafo.Invalidate();
        }

        private void BtnInserirAresta_Click(object sender, EventArgs e)
        {
            if (grafo.Vertices.Count < 2)
            {
                MessageBox.Show("Sao necessarios pelo menos dois vertices para inserir uma aresta.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (FormInserirAresta frmInserirAresta = new FormInserirAresta(grafo.Vertices.ToList()))
            {
                if (frmInserirAresta.ShowDialog() == DialogResult.OK)
                {
                    Vertice verticeOrigem = frmInserirAresta.cmbOrigem.SelectedItem as Vertice;
                    Vertice verticeDestino = frmInserirAresta.cmbDestino.SelectedItem as Vertice;
                    if (verticeOrigem == verticeDestino)
                    {
                        MessageBox.Show("Os vertices de origem e destino nao podem ser iguais.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    grafo.InserirAresta(verticeOrigem, verticeDestino);
                    AtualizarAdjacencia();
                    painelGrafo.Invalidate();
                }
            }
        }
        private void BtnAlterarVertice_Click(object sender, EventArgs e)
        {
            if (!grafo.Vertices.Any())
            {
                MessageBox.Show("Não há vértices para alterar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (FormAlterarVertice frmAlterar = new FormAlterarVertice(grafo.Vertices.ToList()))
            {
                if (frmAlterar.ShowDialog() == DialogResult.OK)
                {
                    Vertice verticeSelecionado = frmAlterar.cmbVertice.SelectedItem as Vertice;
                    string novoNome = frmAlterar.txtNovoNome.Text.Trim();
                    if (string.IsNullOrEmpty(novoNome))
                    {
                        MessageBox.Show("O novo nome não pode ser vazio.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (grafo.Vertices.Any(v => !v.Equals(verticeSelecionado)
                                               && v.Nome.Equals(novoNome, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Já existe um vértice com esse nome.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    grafo.AlterarVertice(verticeSelecionado, novoNome);
                    AtualizarAdjacencia();
                    painelGrafo.Invalidate();
                }
            }
        }
        private void BtnRemoverVertice_Click(object sender, EventArgs e)
        {
            if (!grafo.Vertices.Any())
            {
                MessageBox.Show("Nao ha vertices para remover.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (FormRemoverVertice frmRemover = new FormRemoverVertice(grafo.Vertices.ToList()))
            {
                if (frmRemover.ShowDialog() == DialogResult.OK)
                {
                    Vertice verticeSelecionado = frmRemover.cmbVertice.SelectedItem as Vertice;
                    DialogResult confirmacao = MessageBox.Show("Tem certeza que deseja remover o vertice \"" + verticeSelecionado.Nome + "\" e todas as suas arestas?", "Confirmar Remocao", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (confirmacao == DialogResult.Yes)
                    {
                        grafo.RemoverVertice(verticeSelecionado);
                        AtualizarAdjacencia();
                        painelGrafo.Invalidate();
                    }
                }
            }
        }
        private void BtnRemoverAresta_Click(object sender, EventArgs e)
        {
            if (grafo.Vertices.Count < 2)
            {
                MessageBox.Show("Nao ha arestas para remover.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (FormRemoverAresta frmRemoverAresta = new FormRemoverAresta(grafo.Vertices.ToList()))
            {
                if (frmRemoverAresta.ShowDialog() == DialogResult.OK)
                {
                    Vertice verticeOrigem = frmRemoverAresta.cmbOrigem.SelectedItem as Vertice;
                    Vertice verticeDestino = frmRemoverAresta.cmbDestino.SelectedItem as Vertice;
                    if (verticeOrigem == verticeDestino)
                    {
                        MessageBox.Show("Os vertices de origem e destino nao podem ser iguais.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    Aresta arestaSelecionada = null;
                    foreach (Vertice v in grafo.Vertices)
                    {
                        arestaSelecionada = v.Arestas.FirstOrDefault(a =>
                           a.NomeAresta.Equals(verticeOrigem.Nome + "_para_" + verticeDestino.Nome) ||
                           a.NomeAresta.Equals(verticeDestino.Nome + "_para_" + verticeOrigem.Nome));
                        if (arestaSelecionada != null)
                            break;
                    }
                    if (arestaSelecionada == null)
                    {
                        MessageBox.Show("Aresta nao encontrada.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    DialogResult confirmacao = MessageBox.Show("Tem certeza que deseja remover a aresta \"" + arestaSelecionada.NomeAresta + "\"?", "Confirmar Remocao", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (confirmacao == DialogResult.Yes)
                    {
                        grafo.RemoverAresta(arestaSelecionada);
                        AtualizarAdjacencia();
                        painelGrafo.Invalidate();
                    }
                }
            }
        }
        private void BtnDijkstra_Click(object sender, EventArgs e)
        {
            if (grafo.Vertices.Count < 2)
            {
                MessageBox.Show("Sao necessarios pelo menos dois vertices para calcular o caminho.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (FormDijkstra frmDijkstra = new FormDijkstra(grafo.Vertices.ToList()))
            {
                if (frmDijkstra.ShowDialog() == DialogResult.OK)
                {
                    Vertice verticeOrigem = frmDijkstra.cmbOrigem.SelectedItem as Vertice;
                    Vertice verticeDestino = frmDijkstra.cmbDestino.SelectedItem as Vertice;
                    if (verticeOrigem == verticeDestino)
                    {
                        MessageBox.Show("Os vertices de origem e destino nao podem ser iguais.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    (List<Vertice> caminho, int custo) = grafo.Dijkstra(verticeOrigem, verticeDestino);
                    if (caminho == null)
                        MessageBox.Show("Nao foi possivel encontrar um caminho entre os vertices selecionados.", "Resultado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                    {
                        string resultado = "Caminho minimo: " + string.Join(" -> ", caminho.Select(v => v.Nome)) + Environment.NewLine;
                        resultado += "Custo: " + custo;
                        MessageBox.Show(resultado, "Resultado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        private void BtnExibirAdjacencia_Click(object sender, EventArgs e)
        {
            AtualizarAdjacencia();
            painelGrafo.ResetView();
        }

        private void AtualizarAdjacencia()
        {
            StringWriter escritor = new StringWriter();
            foreach (Vertice vertice in grafo.Vertices)
            {
                escritor.Write(vertice.Nome + " -> ");
                foreach (Aresta aresta in vertice.Arestas)
                {
                    try
                    {
                        Vertice vizinho = aresta.ObterVizinho(vertice);
                        escritor.Write(vizinho.Nome + " ");
                    }
                    catch
                    {
                        escritor.Write("Erro ");
                    }
                }
                escritor.WriteLine();
            }
            txtAdjacencia.Text = escritor.ToString();
        }
    }
    internal static class Program
    {
        [STAThread]
        public static void Main()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmPrincipal());
        }
    }
}
