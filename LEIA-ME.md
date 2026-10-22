# MDPS · AION 2 Global

Leitor para Windows 10/11 de 64 bits com interface em português.

**Versão 1.3.1 · Criado por @misttylol**

## Download e instalação

Na página **Releases** do GitHub, escolha uma destas opções:

- **MDPS-Setup-win-x64.exe:** instalador recomendado. Instala no perfil do usuário e cria um atalho na área de trabalho.
- **MDPS-portatil-win-x64.zip:** versão portátil. Extraia o ZIP inteiro e abra `MDPS.exe`.

Abra o MDPS antes de entrar na dungeon.
Use o jogo em janela ou janela sem bordas para visualizar o medidor por cima.
O executável publicado inclui o .NET; não é preciso instalar o SDK para jogar.
O Npcap é necessário para a leitura ao vivo: https://npcap.com/#download.

## Uso

- **Engrenagem no topo:** escolha **Minha cura** ou **Meu DPS**, selecione entre a cor padrão e cinco cores alternativas e acesse Ao vivo. As escolhas ficam salvas neste computador.
- **Verificar atualização:** procura uma versão mais recente no GitHub, baixa o instalador e abre a atualização automaticamente.
- **Ao vivo:** detecta a conexão do AION 2 e começa a leitura. Aguarda o jogo se estiver fechado.
- **Botão ↺ ao lado da engrenagem:** zera os números do combate mostrado e a cura acumulada na DG. Os combates já salvos continuam no histórico.
- Ao entrar em uma DG diferente, o painel e a cura acumulada zeram automaticamente. Teleportes e recarregamentos dentro da mesma DG não zeram os valores.
- **Sua cura total na DG:** soma a cura que você realizou nos aliados e em si em todos os combates desde a entrada no mapa atual. O total permanece ao iniciar outro combate e ao usar Novo combate; reinicia ao trocar de mapa ou reiniciar a leitura. O HPS ao lado é do combate exibido. A cura recebida de outros jogadores não entra no seu total. O curador pode aparecer mesmo sem causar dano.
- **Meu DPS:** mostra seu dano total e DPS do combate exibido, útil para personagens que não são curadores. O DPS da PT continua no topo nas duas opções.
- A lista acima do nome do chefe permite consultar os últimos 10 combates de DG enquanto a captura continua. Ao selecionar um, aparecem os jogadores registrados naquela PT.
- O emblema da classe aparece à esquerda do nome de cada jogador identificado no ranking.
- Durante uma luta contra boss ou mini boss identificado na DG, o painel mostra o nome, a vida atual e a porcentagem. Quando o jogo envia o tempo limite do boss, aparece também a contagem para derrotá-lo. A barra de vida só aparece com vida máxima confirmada pelo jogo.
- Arraste o cabeçalho para mover a janela. Redimensione pelo canto inferior direito.
- A largura inicial é próxima à do minimapa do AION 2. O MDPS lembra a posição e o tamanho que você deixar.
- A janela compacta fica sobre o jogo. **Ctrl+Shift+L** alterna o modo que deixa os cliques passarem para o AION 2. Para destravar, use o mesmo atalho ou clique com o botão direito no ícone do MDPS ao lado do relógio do Windows e escolha **Mostrar e destravar**. Um clique duplo no ícone também destrava.
- **Ctrl+Shift+D:** mostra ou oculta. **Ctrl+Shift+R:** novo combate.
- Durante o combate o painel atualiza cerca de quatro vezes por segundo; fora do combate, uma vez por segundo.

O medidor conta o dano recebido pelo cliente contra todos os alvos do combate, incluindo inimigos comuns.
Na janela e no histórico, só aparecem você e os membros da PT identificados pela lista do jogo;
o DPS do grupo, o dano total e as porcentagens são recalculados apenas com esses jogadores.
Até a lista da PT chegar, aparece somente você. Abra o leitor antes de entrar na DG para receber os nomes e a lista.
DPS e participação usam o cálculo por encontro do núcleo AionMeter; não são o acumulado de toda a DG.
Uma nova luta é separada automaticamente conforme os eventos de combate, morte, mudança de zona e inatividade.
O resultado anterior permanece na tela até a próxima luta. A duração segue o tempo de combate calculado pelo núcleo.
O gráfico de cada linha compara o dano com o primeiro colocado; o percentual é a participação no dano total.
O total de cura e o HPS de **cada combate** também ficam no histórico. Históricos anteriores a essa função mostram `—`, pois não guardavam cura. O total acumulado da DG fica no painel enquanto o leitor estiver aberto.
HPS usa a mesma duração de combate do DPS. Curas fora de um combate ativo não são contadas.
São valores de cura informados pelo jogo; a versão atual não separa cura efetiva de overheal e não aplica filtro estrito de membros da PT aos destinatários.

## Dados e limitações

O núcleo usa captura passiva da conexão do jogo, sem injetar código nem alterar sua memória.
Este aplicativo não envia resultados, não instala atualizações automaticamente e não grava pacotes brutos.
O histórico fica em `dist/MDPS/user-data/history`, no próprio computador, e não é apagado automaticamente.
As tabelas de nomes de habilidades e chefes usam inglês.

Abra antes da tela de carregamento da DG para que os nomes sejam identificados.
Nomes como `#12345` indicam identificação ainda não recebida e não entram no ranking da PT.
O leitor só consegue contar o dano que o servidor envia ao seu cliente. Perdas na captura, VPNs e mudanças no
protocolo do jogo podem afetar a leitura. A interface mostra erros e ausência de dados; isso não equivale a dano zero.
A compatibilidade real com sua instalação Global precisa ser validada em uma DG.

## Desenvolvimento

Código da interface em `src/MDPS`, núcleo de terceiros em `vendor/AionMeter.Core` e testes em `tests/MDPS.Tests`.
Com SDK .NET 10 disponível, execute `./build.ps1 -Test` para testar e publicar em `dist/MDPS`.
Use `./package.ps1` para gerar o ZIP portátil em `release`. Com Inno Setup instalado, use
`./package.ps1 -Installer` para gerar também o instalador.
`MDPS.exe --self-test` verifica a integração da interface com um combate controlado e salva a prévia em `artifacts`.
`MDPS.exe --smoke-live` verifica a inicialização da captura por cinco segundos e registra seu estado.
Essas verificações não substituem a validação com dados reais do jogo.

Licença GPL-3.0. Fontes e atribuições: CREDITOS.md, LICENSE e data/NOTICE.md.
