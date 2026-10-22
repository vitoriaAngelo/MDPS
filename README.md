<div align="center">

# ✦ MDPS · AION 2 Global ✦

### um leitor delicado de DPS, cura e desempenho da sua PT ♡

[![Versão](https://img.shields.io/github/v/release/vitoriaAngelo/MDPS?style=for-the-badge&label=vers%C3%A3o&color=f4a7bb)](https://github.com/vitoriaAngelo/MDPS/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/vitoriaAngelo/MDPS/total?style=for-the-badge&color=9bd8cf)](https://github.com/vitoriaAngelo/MDPS/releases)
[![Licença GPL-3.0](https://img.shields.io/badge/licen%C3%A7a-GPL--3.0-c9b6e4?style=for-the-badge)](LICENSE)

**Feito com carinho por [@misttylol](https://github.com/misttylol)**  `n`[♡ Twitch · twitch.tv/misttylol](https://www.twitch.tv/misttylol)

</div>

![Prévia do MDPS](src/MDPS/Assets/mdps-preview.png)

## 🌷 sobre o MDPS

O MDPS é um leitor de **DPS e cura da PT para AION 2 Global**, criado para ficar leve, bonito e discreto sobre o jogo. Ele acompanha a batalha em tempo real e mostra somente as pessoas que estão na sua PT.

## ♡ baixar

Baixe a versão mais recente pela página de [**releases**](https://github.com/vitoriaAngelo/MDPS/releases/latest):

| arquivo | escolha |
| --- | --- |
| **MDPS-Setup-win-x64.exe** | recomendada · instala e cria um atalho na área de trabalho |
| **MDPS-portatil-win-x64.zip** | portátil · extraia o ZIP inteiro antes de abrir `MDPS.exe` |

O MDPS já inclui o .NET necessário. Para ler o combate ao vivo, instale também o [Npcap](https://npcap.com/#download/).

## ✧ como usar

1. Instale o **Npcap**.
2. Abra o MDPS pelo atalho da área de trabalho.
3. Abra o AION 2 em modo janela ou janela sem bordas.
4. Deixe o leitor aberto **antes de entrar na DG**, para identificar corretamente a PT.
5. Entre na DG e comece o combate. O painel mostrará os dados da PT em tempo real.

## ✿ o que aparece no painel

- **DPS da PT** · dano médio por segundo de todos os membros registrados.
- **Dano total** · todo o dano causado durante a DG atual.
- **Minha cura** · total curado por você e HPS do combate.
- **Meu DPS** · seu dano total e dano médio por segundo.
- **Lista da PT** · classe, nome, DPS e participação de cada jogador.
- **Boss e mini boss** · nome, vida, porcentagem e tempo limite quando disponíveis.
- **Histórico** · os 10 combates mais recentes, com a PT registrada em cada um.
- **Minhas skills** · dano, acertos, participação e média por habilidade.

## ☁ botões e configurações

- **⚙ Engrenagem** · troca entre cura e DPS, muda a cor, ativa o modo compacto e verifica atualizações.
- **↺ Zerar** · limpa os números atuais.
- **— Minimizar** · minimiza o painel para a bandeja do Windows.
- **× Fechar** · fecha o MDPS.
- **Modo compacto** · mostra chefe, PT, DPS e cura ocupando pouco espaço.
- **Overlay automático** · recolhe fora da DG e abre ao entrar.

Ao entrar em outra DG ou sair da DG, os números acumulados são reiniciados. Começar um boss ou mini boss dentro da mesma DG não apaga o dano nem a cura.

## 🎀 atalhos

| atalho | ação |
| --- | --- |
| `Ctrl + Shift + D` | mostrar ou ocultar o MDPS |
| `Ctrl + Shift + L` | fixar ou destravar o painel |
| `Ctrl + Shift + R` | zerar o combate atual |

Se o painel estiver fixado, clique duas vezes no ícone do MDPS perto do relógio do Windows para mostrar e destravar.

## 🌙 privacidade e observações

- O leitor conta somente os dados enviados pelo servidor ao seu computador.
- Abra o MDPS antes da tela de carregamento da DG para identificar melhor toda a PT.
- O programa não injeta código, não altera a memória do AION 2 e não envia resultados para servidores.
- O histórico e as configurações ficam somente no computador da pessoa.
- A cura informada pelo jogo pode incluir overheal.

Mais detalhes estão em [**LEIA-ME.md**](LEIA-ME.md).

## 🛠 desenvolvimento

O código é distribuído sob a licença **GPL-3.0**.

```powershell
./build.ps1 -Test
./package.ps1 -Installer
```

<div align="center">♡ obrigada por usar o MDPS ♡</div>

