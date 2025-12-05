# Dark Arms Prototype - Raylib C#

FPS Roguelike avec système d'armes organiques évolutives, inspiré de Dark Arms (Neo Geo Pocket) et Binding of Isaac.

## Setup

```bash
# Restore dependencies
dotnet restore

# Run the game
dotnet run

# Build release
dotnet build -c Release
```

## Contrôles

- **WASD** - Déplacement
- **Souris** - Viser
- **Clic gauche** - Tirer
- **E** - Évoluer l'arme (quand disponible)
- **ESC** - Quitter

## Architecture

```
DarkArmsProto/
├── Program.cs              # Entry point
├── Game.cs                 # Game loop principal
├── Player/
│   └── PlayerController.cs # FPS controller
├── Weapons/
│   ├── WeaponSystem.cs     # Système d'évolution
│   └── Projectile.cs       # Projectiles
├── Enemies/
│   ├── Enemy.cs            # Ennemis
│   └── EnemySpawner.cs     # Spawn system
└── Souls/
    ├── Soul.cs             # Pickups
    └── SoulManager.cs      # Collection system
```

## Système d'évolution

### Types d'âmes
- **Beast** (Orange) - Vitesse et instinct
- **Undead** (Vert) - Drain de vie et poison
- **Demon** (Rouge) - Feu et explosion

### Armes Stage 1
- **Flesh Pistol** (base) - Tir équilibré
- Kill 10 ennemis → Évolution disponible

### Armes Stage 2 (10 souls)
- **Bone Revolver** (Beast) - Gros dégâts, lent, piercing
- **Tendril Burst** (Undead) - Shotgun, lifesteal
- **Parasite Swarm** (Demon) - Rapide, homing

### Armes Stage 3 (25 souls)
- **Apex Predator** (Beast) - Dégâts massifs, instakill
- **Necrotic Cannon** (Undead) - AOE poison
- **Inferno Beast** (Demon) - Burst fire, chain reaction

## Gameplay Loop

1. Kill enemies → Drop souls
2. Collect souls → Feed weapon
3. Weapon evolves → New abilities
4. Dominant soul type → Evolution path

## Tweaking

Modifie facilement les valeurs dans les classes :

**WeaponSystem.cs**
```csharp
private int[] requiredSouls = { 10, 25, 50 };  // Seuils d'évolution
private float damage = 20f;                     // Dégâts de base
private float fireRate = 3f;                    // Tirs par seconde
```

**PlayerController.cs**
```csharp
private float moveSpeed = 5f;           // Vitesse de déplacement
private float mouseSensitivity = 0.003f; // Sensibilité souris
```

**Enemy.cs**
```csharp
maxHealth = 30f;    // HP des ennemis
speed = 3f;         // Vitesse de déplacement
```

## Prochaines étapes

### Features à ajouter :
- [ ] Génération procédurale de rooms
- [ ] Boss fights
- [ ] Plus de types d'ennemis
- [ ] Patterns d'attaque ennemis
- [ ] Items passifs (type Binding of Isaac)
- [ ] Mutations visuelles des armes
- [ ] Particle effects
- [ ] Sound effects
- [ ] Mini-map
- [ ] Menu pause / game over

### Améliorations visuelles :
- [ ] Weapon model animé
- [ ] Blood splatter
- [ ] Muzzle flash
- [ ] Screen shake sur tir
- [ ] Damage numbers
- [ ] Evolution animation

### Optimisations :
- [ ] Object pooling pour projectiles
- [ ] Spatial partitioning pour collisions
- [ ] LOD pour ennemis distants

## Port vers Unity

Cette architecture est facilement transférable vers Unity :
- `PlayerController` → MonoBehaviour avec CharacterController
- `WeaponSystem` → ScriptableObject pour les évolutions
- `Enemy` → NavMeshAgent pour le pathfinding
- Même logique de game loop

## Notes de design

L'évolution se base sur le type d'âme **dominant**, pas sur un choix manuel. 
Cela encourage le joueur à :
- Cibler certains types d'ennemis
- Planifier son build
- Adapter sa stratégie

Alternative : Ajouter un menu de choix comme dans le prototype Three.js.