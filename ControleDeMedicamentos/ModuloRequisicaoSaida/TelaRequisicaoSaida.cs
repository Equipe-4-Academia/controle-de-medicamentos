﻿using ControleDeMedicamentos.Compartilhado;
using ControleDeMedicamentos.ModuloFornecedor;
using ControleDeMedicamentos.ModuloMedicamento;
using ControleDeMedicamentos.ModuloPaciente;
using ControleDeMedicamentos.ModuloPrescricaoMedica;
using ControleDeMedicamentos.ModuloRequisicaoEntrada;
using ControleDeMedicamentos.Util;
using System.Security.Cryptography.X509Certificates;

namespace ControleDeMedicamentos.ModuloRequisicaoSaida;

public class TelaRequisicaoSaida : TelaBase<RequisicaoSaida>, ITelaCrud
{
    public IRepositorioRequisicaoSaida repositorioRequisicaoSaida;
    public IRepositorioPrescricaoMedica repositorioPrescricaoMedica;
    public IRepositorioMedicamento repositorioMedicamento;
    public IRepositorioPaciente repositorioPaciente;

    public TelaRequisicaoSaida(IRepositorioPrescricaoMedica repositorioPrescricaoMedica, IRepositorioMedicamento repositorioMedicamento, IRepositorioPaciente repositorioPaciente, IRepositorioRequisicaoSaida repositorioRequisicaoSaida) : base("Requisição de Saida", repositorioRequisicaoSaida)
    {
        this.repositorioPrescricaoMedica = repositorioPrescricaoMedica;
        this.repositorioPaciente = repositorioPaciente;
        this.repositorioMedicamento = repositorioMedicamento;
        this.repositorioRequisicaoSaida = repositorioRequisicaoSaida;
    }

    public void ApresentarMenuSaida()
    {
        ExibirCabecalho();

        Console.WriteLine();

        Console.WriteLine($"[1] Cadastrar requisição de saída");
        Console.WriteLine($"[2] Visualizar requisições de saída");
        Console.WriteLine($"[3] Visualizar requisições por paciente");
        Console.WriteLine("[S] Voltar");

        Console.Write("\nEscolha uma das opções: ");
        string opcao = Console.ReadLine() ?? string.Empty;
        if(opcao.Length > 0)
        {
            char operacaoEscolhida = Convert.ToChar(opcao[0]);
            Opcoes(operacaoEscolhida);
        }
        else
        {
            Notificador.ExibirMensagem("Entrada Invalida! vefirique a opção digitada e tente novamente.", ConsoleColor.Red);
            ApresentarMenuSaida();
        }
    }

    public void Opcoes(char opcao)
    {
        if (opcao == '1') CadastrarRegistro();
        else if (opcao == '2') VisualizarRegistros(true);
        else if (opcao == '3') VisualizarRegistrosPorPaciente();
    }
    public override RequisicaoSaida ObterDados()
    {
        if (repositorioPaciente.SelecionarTodos().Count == 0)
        {
            Notificador.ExibirMensagem("Não há um paciente registrado, cadastre um paciente no Menu Pacientes", ConsoleColor.Red);
            return null;
        }
        else if (repositorioPrescricaoMedica.SelecionarTodos().Count == 0)
        {
            Notificador.ExibirMensagem("Não há uma prescrição registrada, cadastre uma prescrição no menu Prescrição", ConsoleColor.Red);
            return null;
        }
        Console.Write("Digite a data da Solicitação(dd/MM/yyyy): ");
        string datastring = Console.ReadLine()!;
        DateTime? data = Convertor.ConverterStringParaDate(datastring);
        if (data == null) return null;
        Console.WriteLine();        
        TelaPaciente telaPaciente = new TelaPaciente(repositorioPaciente);
        telaPaciente.VisualizarRegistros(false);
        Console.Write("Digite o Id do Paciente que deseja fazer uma requisição: ");
        int idPaciente = Convertor.ConverterStringParaInt();
        if (idPaciente == 0) return null;
        Console.WriteLine();

        Paciente paciente = repositorioPaciente.SelecionarRegistroPorId(idPaciente);

        if (paciente == null)
        {
            Notificador.ExibirMensagem("paciente não encontrado!", ConsoleColor.Red);
            return null;
        }
        TelaPrescricaoMedica telaPrescricaoMedica = new TelaPrescricaoMedica(repositorioMedicamento, repositorioPrescricaoMedica);
        telaPrescricaoMedica.VisualizarRegistros(false);
        Console.Write("Digite o Id da Prescrição Médica: ");
        int idPrescricao = Convertor.ConverterStringParaInt();
        if (idPrescricao == 0) return null;
        Console.WriteLine();
        PrescricaoMedica prescricao = repositorioPrescricaoMedica.SelecionarRegistroPorId(idPrescricao);
        List<RequisicaoSaida> requisicoes = repositorioRequisicaoSaida.SelecionarTodos();
        int e = 0;
        foreach (var p in requisicoes)
        {
            
            if (p.Id == prescricao.Id)
            {
                Notificador.ExibirMensagem("Não é possível fazer uma requisição com uma prescrição já usada", ConsoleColor.Red);
                return null;
            }
                e++;
        }

        if (prescricao == null)
        {
            Notificador.ExibirMensagem("Prescrição médica não encontrada!", ConsoleColor.Red);
            return null;
        }
        List<int> quantidadeDeMedicamentos = new List<int>();
        for (int i = 0; i < prescricao.MedicamentosDaPrescricao.Count; i++)
        {
            Console.WriteLine();
            Medicamento m = prescricao.MedicamentosDaPrescricao[i];
            Console.WriteLine($"A quantidade em Estoque de {m.NomeMedicamento} é de {m.Quantidade}");
            Console.Write($"Digite a quantidade de medicamentos que você deseja: ");
            int quantidade = Convertor.ConverterStringParaInt();
            if (quantidade == 0) return null;
            if (quantidade > m.Quantidade)
            {
                Notificador.ExibirMensagem("Não há medicamentos suficientes desse remédio", ConsoleColor.Red);
                i--;
                continue;
            }
            else
            {
                quantidadeDeMedicamentos.Add(quantidade);
                m.RemoverEstoque(quantidade);
            }
        } 
        
        RequisicaoSaida requisicaoSaida = new RequisicaoSaida(data, idPaciente, idPrescricao, quantidadeDeMedicamentos, prescricao.MedicamentosDaPrescricao);

        Console.WriteLine();
        for (int i = 0; i < prescricao.MedicamentosDaPrescricao.Count; i++)
        {
            Medicamento m = prescricao.MedicamentosDaPrescricao[i];
            if (m.Quantidade < 20)
            {
                Notificador.ExibirCorDeFonte($"Medicamento {m.NomeMedicamento} com estoque menor que 20un. Marcado como: 'Em Falta.'", ConsoleColor.DarkYellow);
                Notificador.ExibirCorDeFonte($"Recomenda-se uma nova Requisição de entrada do Medicamento {m.NomeMedicamento} antes que o estoque chegue a 0", ConsoleColor.Yellow);
            }
            Console.WriteLine();
            if(m.Quantidade == 0)
            {
                Notificador.ExibirCorDeFonte($"Medicamento {m.NomeMedicamento} sem estoque..'", ConsoleColor.Red);
                Notificador.ExibirCorDeFonte($"Recomenda-se uma nova Requisição de entrada do Medicamento {m.NomeMedicamento}", ConsoleColor.Red);
            }
        }

        return requisicaoSaida;

    }
    public override void VisualizarRegistros(bool exibirTitulo)
    {
        if (exibirTitulo) ExibirCabecalho();

        Console.WriteLine("Visualizando Requisições de Saída...");
        Console.WriteLine("--------------------------------------------");

        Console.WriteLine();
        List<RequisicaoSaida> requisicoes = repositorioRequisicaoSaida.SelecionarTodos();
        VisualiarRequisicoes(requisicoes);
        Notificador.ExibirMensagem("Pressione ENTER para continuar...", ConsoleColor.DarkYellow);
    }
    public void VisualizarRegistrosPorPaciente()
    {
        ExibirCabecalho();

        Console.WriteLine();
        TelaPaciente telaPaciente = new TelaPaciente(repositorioPaciente);
        telaPaciente.VisualizarRegistros(false);
        Console.Write("Digite o Id do Paciente: ");
        int idPaciente = Convertor.ConverterStringParaInt();
        if (idPaciente == 0) return;

        Paciente paciente = repositorioPaciente.SelecionarRegistroPorId(idPaciente);
        if (paciente == null) return;
        Console.WriteLine();
        List<RequisicaoSaida> requisicaoSaidas = new List<RequisicaoSaida>();
        int contador = 0;
        foreach (var i in repositorioRequisicaoSaida.SelecionarTodos())
        {
            if ((i.pacienteId == paciente.Id))
            {
                i.Id = ++contador;
                requisicaoSaidas.Add(i);
            }
            
        }
        if(requisicaoSaidas.Count == 0)
        {
            Notificador.ExibirMensagem("Esse paciente não tem nenhuma requisição de saída", ConsoleColor.Red);
            return;
        }
        VisualiarRequisicoes(requisicaoSaidas);
    }
    public void VisualiarRequisicoes(List<RequisicaoSaida> requisicoes)
    {
        Console.WriteLine(
            "{0, -6} | {1, -10} | {2, -20} | {3, -20}",
            "Id", "Data", "Paciente", "Quant Remédios Selecionados"
        );
        
        foreach (var r in requisicoes)
        {
            Console.WriteLine(
            "{0, -6} | {1, -10} | {2, -20} | {3, -20}",
            r.Id, r.Data.ToString("dd/MM/yyyy"), repositorioPaciente.SelecionarRegistroPorId(r.pacienteId).Nome, r.MedicamentosRequisitados.Count
            );
        }
        Console.Write("\nDeseja ver alguma requisição em detalhes(s/n)? ");
        string opcao = Console.ReadLine()!.ToUpper();
        Console.WriteLine();
        if (opcao == "S")
        {
            Console.Write("Digite o id da Requisição que deseja ver em detalhes: ");
            int id = Convertor.ConverterStringParaInt();
            if (id == 0) return;
            Console.WriteLine();
            Console.WriteLine(
            "{0, -12} | {1, -10}",
            "Medicamento", "Quantidade"
            );
            int posicao = 0;
            if (id <= 0 || id - 1 >= requisicoes.Count || requisicoes[id - 1] == null)
            {
                Notificador.ExibirMensagem("Essa requisição não existe, retornando", ConsoleColor.Red);
                return;
            }

            RequisicaoSaida r = requisicoes[id - 1];
            foreach (var d in r.MedicamentosRequisitados)
            {

                Console.WriteLine(
                "{0, -12} | {1, -10}",
                r.medicamentosstring[posicao], r.QuantidadeDeMedicamentos[posicao]
                );
                posicao += 1;
            }
       }
    }
}