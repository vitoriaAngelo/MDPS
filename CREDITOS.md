# MDPS 0.1

Interface própria em português para AION 2 Global, criada em 22/10/2026.
Os emblemas das classes em `src/MDPS/Assets/Classes` foram copiados do projeto AionMeter citado abaixo.

O núcleo em `vendor/AionMeter.Core` foi originalmente copiado de
https://github.com/cyberbadger6969/aion2-dps-meter,
commit `13ede2e86497933f4a629c0abc0335f2d68b4211`.
Ele fornece captura passiva, reconstrução TCP, interpretação do protocolo e cálculo dos combates.
Alterações locais de 22/10/2026: contagem de cura sem exigir dano do curador,
identificação de habilidades de cura no fluxo de dano, exposição de cura/HPS nos resultados
e eventos de cura simulados no modo de demonstração.
Os testes desse núcleo também foram preservados em `tests/MDPS.Tests`; `CsvTests.cs` testa a exportação própria do MDPS.
MDPS é um trabalho derivado distribuído sob GPL-3.0. Veja LICENSE.
Não é produto oficial da NCSOFT.

As tabelas de jogo e suas fontes estão documentadas em `data/NOTICE.md`.
SharpPcap e K4os.Compression.LZ4 são dependências NuGet do núcleo.
Npcap é um componente externo instalado separadamente, não distribuído com MDPS.
O código-fonte correspondente está nesta pasta; ao redistribuir binários,
disponibilize também o código e as licenças conforme a GPL-3.0.
