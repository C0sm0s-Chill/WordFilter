# WordFilter for TShock 5.2.4

**[🇬🇧 English](#english)** | **[🇫🇷 Français](#français)**

---

<a name="english"></a>
## 🇬🇧 English

Chat filtering plugin with progressive sanctions system and chat bubbles.

### Features

- ✅ **Automatic word filtering** with custom replacements
- ✅ **Chat bubbles** above player heads  
- ✅ **Progressive sanctions**: Warnings → Kick → Temporary ban
- ✅ **Persistent database** (SQLite/MySQL)
- ✅ **Thread-safe** and optimized with compiled regex

### Quick Start

1. Place `WordFilter.dll` in `ServerPlugins` folder
2. Restart server
3. Add filtered words: `/wf add badword ***`

### Basic Commands

```bash
/wf add <word> <replacement>    # Add filtered word
/wf remove <word>               # Remove word
/wf list                        # List all filtered words
/wf warnings                    # View all warnings
/wf warnings <player>           # Check specific player
/wf resetwarnings <player>      # Reset player warnings
/wf config                      # View configuration
/wf reload                      # Reload the plugin
```

### Configuration

Edit `WordFilterConfig.json`:

```json
{
  "ChatAboveHead": true,
  "EnableWarnings": true,
  "WarningsBeforeKick": 3,
  "WarningsBeforeBan": 5,
  "BanDurationMinutes": 60,
  "ResetWarningsAfterMinutes": 30,
  "ImmunityGroups": ["owner","superadmin", "admin"]
}
```

### How It Works

**Example scenario (default settings):**

1. **Warnings 1-2**: `"Warning 1/3 before kick..."`
2. **Warning 3**: Player kicked (warnings persist)
3. Player reconnects
4. **Warning 4**: `"Warning 1/2 before BAN (60 minutes)..."`
5. **Warning 5**: Player banned for 60 minutes

**Auto-reset**: Warnings reset after 30 minutes of good behavior.

### Permissions

```bash

- wordfilter.manage
```

### Technical Details

- TShock 5.2.4 | .NET 9
- Thread-safe with optimized regex
- Database: SQLite/MySQL (TShock DB)

---

<a name="français"></a>
## 🇫🇷 Français

Plugin de filtrage de chat avec système de sanctions progressives et bulles de chat.

### Fonctionnalités

- ✅ **Filtrage automatique** avec remplacements personnalisés
- ✅ **Bulles de chat** au-dessus des joueurs
- ✅ **Sanctions progressives** : Avertissements → Kick → Ban temporaire
- ✅ **Base de données persistante** (SQLite/MySQL)
- ✅ **Thread-safe** et optimisé avec regex compilées

### Démarrage Rapide

1. Placez `WordFilter.dll` dans le dossier `ServerPlugins`
2. Redémarrez le serveur
3. Ajoutez des mots filtrés : `/wf add motgrossier ***`

### Commandes de Base

```bash
/wf add <mot> <remplacement>    # Ajouter mot filtré
/wf remove <mot>                # Supprimer mot
/wf list                        # Lister tous les mots
/wf warnings                    # Voir tous les avertissements
/wf warnings <joueur>           # Vérifier un joueur
/wf resetwarnings <joueur>      # Réinitialiser avertissements
/wf config                      # Voir la configuration
/wf reload                      # Recharger le plugin
```

### Configuration

Éditez `WordFilterConfig.json` :

```json
{
  "ChatAboveHead": true,
  "EnableWarnings": true,
  "WarningsBeforeKick": 3,
  "WarningsBeforeBan": 5,
  "BanDurationMinutes": 60,
  "ResetWarningsAfterMinutes": 30,
  "ImmunityGroups": ["owner","superadmin", "admin"]
}
```

### Fonctionnement

**Exemple de scénario (paramètres par défaut) :**

1. **Avertissements 1-2** : `"Avertissement 1/3 avant kick..."`
2. **Avertissement 3** : Joueur kick (avertissements persistent)
3. Le joueur se reconnecte
4. **Avertissement 4** : `"Avertissement 1/2 avant BAN (60 minutes)..."`
5. **Avertissement 5** : Joueur banni pour 60 minutes

**Auto-reset** : Les avertissements se réinitialisent après 30 minutes de bon comportement.

### Permissions

```bash

- wordfilter.manage
```

### Détails Techniques

- TShock 5.2.4 | .NET 9
- Thread-safe avec regex optimisées
- Base de données : SQLite/MySQL (DB TShock)

---
