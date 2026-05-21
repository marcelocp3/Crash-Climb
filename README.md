# Crash-Climb 
Esse repositório será utilizado para o desenvolvimento de um jogo de categoria foddian (Jogo de plataforma). Feito com Unity


## Nome do Jogo: Crash & Climb
## Integrantes:
Marcelo da Costa Poltronieri e Raymond Lisbona


## Qual o gênero do jogo: 
Jogo de plataforma

## Um breve descritivo sobre o jogo: 
Jogo 2D de plataforma onde o jogador usa saltos
oscilantes para escalar uma torre formada por superfícies com comportamentos únicos.
Pedra firme, gelo escorregadio, cola que pula menos, cristais que alteram a gravidade e
rochas que quebram criam desafios variados. O diferencial está na combinação entre o
timing do salto e a reação das superfícies, tornando cada avanço uma conquista e cada
queda uma lição.

## Sprint 2 - Protótipo Unity

Scripts adicionados em `Assets/Scripts`:

- `CrashClimbPlayerController2D.cs`: movimento horizontal, salto carregado/oscilante, ataque, dano, respawn e parâmetros de animação.
- `CrashClimbSurface2D.cs`: superfícies de pedra, gelo, cola, cristal de gravidade e rocha quebrável.
- `CrashClimbSpikeHazard2D.cs`: espinho como inimigo ambiental, causando dano e knockback ao jogador.
- `CrashClimbProceduralMap2D.cs`: gera uma torre 2D jogável com plataformas variadas, paredes laterais, spawn e objetivo.
- `CrashClimbCameraFollow2D.cs`: câmera ortográfica seguindo o jogador.

Como testar em uma cena nova da Unity:

1. Aperte Play. Se a cena estiver vazia, `CrashClimbBootstrap2D` cria um `GameManager` automaticamente.
2. O `GameManager` recebe `CrashClimbProceduralMap2D`, que cria câmera, background, plataformas, player e mapa.
3. Controles padrão: setas/A-D para movimento, segurar e soltar Espaço/W/Seta para cima para pular.

Observação: como o jogo não possui inimigos tradicionais, os espinhos entram como inimigos ambientais para a Sprint 2.

## Sprint 3 - Level Design, Menu e HUD

Itens implementados:

- Torre completa com 42 plataformas divididas em zonas de desafio: pedra, gelo, cola, rochas quebráveis, cristais e topo final.
- Player integrado automaticamente ao mapa, com câmera seguindo e respawn no início.
- Menu inicial com arte do background/personagem, botões de jogar, reiniciar mapa e sair.
- Tela de conclusão ao chegar no topo da torre.
- HUD atualizado com vida, carga do salto, progresso de altura e zona atual.
- Áudio de menu, gameplay, pulo, pouso em gelo/gosma, dano e quebra de plataforma carregado por `Resources/CrashClimb/Audio`.
- Cenas `Assets/MainMenu.unity`, `Assets/Main.unity` e `Assets/GameComplete.unity` registradas no Build Settings, com transição menu -> jogo -> conclusão.

Fluxo de cenas: `MainMenu` abre o menu final, `Main` executa o mapa jogável e `GameComplete` mostra a tela de conclusão.
Imagem final do menu: salvar o PNG em `Assets/Resources/CrashClimb/Menu/MenuBackground.png`. O menu carrega esse arquivo automaticamente quando existir.
