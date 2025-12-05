# Dark Arms Prototype - Raylib C#

FPS Roguelike avec syst�me d'armes organiques �volutives, inspir� de Dark Arms (Neo Geo Pocket) et Binding of Isaac.

## Setup
`ash
# Restore dependencies
dotnet restore

# Run the game
dotnet run

# Build release
dotnet build -c Release
`

## Contr�les

- **WASD** - D�placement
- **Souris** - Viser
- **Clic gauche** - Tirer
- **E** - �voluer l'arme (quand disponible)
- **ESC** - Quitter

## Architecture (ECS-lite)
Le projet a �t� refactoris� vers une architecture orient�e composants (GameObject / Component).

`
DarkArmsProto/
 Program.cs              # Entry point
 Game.cs                 # Game loop principal
 GameConfig.cs           # Configuration globale
 Core/
    GameObject.cs       # Entit� de base (Position, Components)
    Component.cs        # Classe de base des composants
 Components/
    PlayerInputComponent.cs # Gestion des inputs et mouvements
    CameraComponent.cs      # Gestion de la caméra 3D
    ProjectileComponent.cs  # Logique des projectiles
    HealthComponent.cs      # Vie et dégâts
    EnemyComponent.cs       # Logique spécifique aux ennemis
    SoulComponent.cs        # Comportement des âmes
    ChaseAIComponent.cs     # IA de poursuite
    MeshRendererComponent.cs # Rendu 3D simple
 World/
    RoomManager.cs      # Gestion du donjon et des transitions
    Room.cs             # Données d'une salle
    Door.cs             # Logique des portes
 Weapons/
    WeaponSystem.cs     # Système d'évolution des armes
 Enemies/
    EnemySpawner.cs     # Factory pour cr�er les ennemis
 Souls/
     SoulManager.cs      # Gestionnaire des �mes
`

## Syst�me d'�volution

### Types d'�mes
- **Beast** (Orange) - Vitesse et instinct
- **Undead** (Vert) - Drain de vie et r�g�n�ration
- **Demon** (Rouge) - D�g�ts rapides et homing

### Armes Stage 1
- **Flesh Pistol** (base) - Tir �quilibr� standard
- Collecte 10 souls  �volution disponible

### Armes Stage 2 (10 souls)
- **Bone Revolver** (Beast) - Gros projectile orange, piercing, d�g�ts �lev�s
- **Tendril Burst** (Undead) - Shotgun 5 projectiles, lifesteal actif
- **Parasite Swarm** (Demon) - Projectiles rapides avec homing automatique

### Armes Stage 3 (25 souls)
- **Apex Predator** (Beast) - D�g�ts massifs, gros projectiles
- **Necrotic Cannon** (Undead) - Shotgun am�lior�, lifesteal x2
- **Inferno Beast** (Demon) - Tir ultra-rapide avec homing

## Features impl�ment�es

###  Gameplay Core
- Architecture ECS modulaire
- FPS controller fluide
- Syst�me de tir avec projectiles physiques
- 3 types d'ennemis avec AI (chase player)
- Collection d'�mes avec magn�tisme
- �volution d'arme bas�e sur �me dominante
- Comportements d'armes diff�renci�s (piercing, shotgun, homing)

###  Visual Feedback
- **Screen shake** sur tir
- **Hit flash** blanc sur ennemis touch�s
- **Damage numbers** flottants en 3D
- **Health bars** dynamiques sur ennemis
- **Glow effects** sur projectiles et souls
- **UI compl�te** avec barres d'�mes par type

## Tweaking rapide

### GameConfig.cs
La plupart des valeurs d'�quilibrage sont centralis�es ici :
`csharp
public const float BaseDamage = 20f;
public const float BaseFireRate = 3f;
public const float ScreenShakeIntensity = 0.2f;
`

### Components/PlayerInputComponent.cs
`csharp
public float MoveSpeed { get; set; } = 10f;
public float MouseSensitivity { get; set; } = 0.003f;
`

### Enemies/EnemySpawner.cs
Les stats des ennemis sont d�finies lors de leur cr�ation :
`csharp
float hp = (type == SoulType.Beast) ? 150 : 100;
float speed = (type == SoulType.Beast) ? 6 : 4;
`

## Prochaines �tapes

###  High Priority
- [ ] **G�n�ration proc�durale de rooms** (type Binding of Isaac)
- [ ] **Boss fights** avec patterns d'attaque
- [ ] **Plus de types d'ennemis** (shooters, chargers, tanks)
- [ ] **Sound effects** (tir, hit, soul collect, evolution)

###  Polish
- [ ] Weapon model visible (main gauche avec animation)
- [ ] Evolution animation (transformation visuelle)
- [ ] Blood splatter decals
- [ ] Mini-map avec fog of war

## Notes techniques

### Raylib-cs 7.0 API
Le projet utilise Raylib-cs 7.0.

### Architecture
L'architecture actuelle est un ECS "lite" (Entity Component System) personnalis�.
- **GameObject** : Conteneur d'une position et d'une liste de composants.
- **Component** : Bloc de logique ou de donn�es (Render, Update).
- **System** : Les managers (WeaponSystem, RoomManager) orchestrent le jeu.

Cette approche facilite l'ajout de nouvelles fonctionnalit�s sans modifier les classes existantes.

## Cr�dits

Inspir� de :
- **Dark Arms: Beast Buster 1999** (Neo Geo Pocket Color)
- **The Binding of Isaac**
- **Vampire Survivors**
