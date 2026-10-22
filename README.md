# MDPS · AION 2 Global

Leitor de DPS e cura da PT para **AION 2 Global**, com painel compacto para ficar sobre o jogo.

**Versão atual: 1.2.1 · Criado por @misttylol**

![Prévia do MDPS](src/MDPS/Assets/mdps-preview.png)

## Download

Baixe sempre pela página de [versões do MDPS](https://github.com/vitoriaAngelo/MDPS/releases/latest):

- **MDPS-Setup-win-x64.exe:** opção recomendada. Instala o programa e cria um atalho na área de trabalho.
- **MDPS-portatil-win-x64.zip:** versão portátil. Extraia o ZIP inteiro antes de abrir `MDPS.exe`.

O MDPS inclui o .NET necessário. Para ler o combate ao vivo, instale também o
[Npcap](https://npcap.com/#download/).

## Como usar

1. Instale o **Npcap**.
2. Abra o MDPS pelo atalho da área de trabalho.
3. Abra o AION 2 em modo janela ou janela sem bordas.
4. Deixe o leitor aberto **antes de entrar na DG**, para receber corretamente os nomes da PT.
5. Entre na DG e inicie o combate. O painel mostrará apenas você e os membros identificados da sua PT.

## Informações mostradas

- **DPS da PT:** dano médio por segundo de todos os membros registrados.
- **Dano total:** todo o dano causado durante a sessão atual da DG.
- **Minha cura:** total curado por você durante a DG e o HPS do combate.
- **Meu DPS:** seu dano total e seu dano médio por segundo.
- **Lista da PT:** classe, nome, DPS e participação de cada jogador.
- **Boss ou mini boss:** nome, vida atual, porcentagem e tempo limite quando o jogo fornece esses dados.
- **Histórico:** os 10 combates de DG mais recentes, incluindo a PT registrada em cada um.

## Botões e configurações

- **⚙ Engrenagem:** alterna entre Minha cura e Meu DPS, muda a cor, ativa o modo compacto e verifica atualizações.
- **↺ Zerar:** limpa os números atuais manualmente.
- **— Minimizar:** minimiza o painel.
- **× Fechar:** fecha o MDPS.
- **Modo compacto:** reduz o painel para mostrar chefe, PT, DPS e cura ocupando pouco espaço.
- **Overlay automático:** fica como uma pequena barra fora da DG e abre sozinho ao entrar.
- **Minhas skills:** mostra quanto cada habilidade sua causou durante a DG, com acertos e participação.
- **Verificar atualização:** baixa e abre automaticamente a versão mais recente publicada no GitHub.

Ao entrar em outra DG ou sair da DG, os números acumulados são reiniciados. Iniciar um boss ou mini boss dentro da
mesma DG não apaga o dano nem a cura acumulados.

## Atalhos

- **Ctrl + Shift + D:** mostra ou oculta o MDPS.
- **Ctrl + Shift + L:** fixa ou destrava o painel para os cliques passarem ao jogo.
- **Ctrl + Shift + R:** zera o combate atual.

Se o painel estiver fixado e não aceitar cliques, clique duas vezes no ícone do MDPS próximo ao relógio do Windows,
ou clique nele com o botão direito e escolha **Mostrar e destravar**.

## Observações

- O leitor só conta os dados que o servidor envia ao seu computador.
- Abra o MDPS antes da tela de carregamento da DG para aumentar a chance de identificar toda a PT.
- O programa não injeta código no jogo, não altera a memória do AION 2 e não envia seus resultados para servidores.
- O histórico e as configurações ficam armazenados apenas no computador da pessoa.
- A cura informada pelo jogo pode incluir overheal.

Mais detalhes técnicos e limitações estão em [LEIA-ME.md](LEIA-ME.md).

## Desenvolvimento

O código é distribuído sob a licença GPL-3.0. Para testar e compilar:

```powershell
./build.ps1 -Test
```

Para gerar o ZIP e o instalador:

```powershell
./package.ps1 -Installer
```
