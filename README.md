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

1. Crie um GameObject vazio chamado `GameManager`.
2. Adicione nele o componente `CrashClimbProceduralMap2D`.
3. Aperte Play. O script cria câmera, player básico e mapa automaticamente.
4. Controles padrão: setas/A-D para movimento, segurar e soltar Espaço/W/Seta para cima para pular, Ctrl esquerdo/mouse 0 para ataque.

Observação: como o jogo não possui inimigos tradicionais, os espinhos entram como inimigos ambientais para a Sprint 2.
