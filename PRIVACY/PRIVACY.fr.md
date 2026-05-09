# LumiFinder — Politique de confidentialité

**Dernière mise à jour : 9 mai 2026**

<p align="center">
  <a href="../PRIVACY.md">English</a> |
  <a href="PRIVACY.ko.md">한국어</a> |
  <a href="PRIVACY.ja.md">日本語</a> |
  <a href="PRIVACY.zh-CN.md">简体中文</a> |
  <a href="PRIVACY.zh-TW.md">繁體中文</a> |
  <a href="PRIVACY.de.md">Deutsch</a> |
  <a href="PRIVACY.es.md">Español</a> |
  <strong>Français</strong> |
  <a href="PRIVACY.pt.md">Português</a>
</p>

---

## Aperçu

LumiFinder (« l'App ») est une application d'explorateur de fichiers pour Windows développée par LumiBear Studio. Nous nous engageons à protéger votre vie privée. Cette politique explique quelles données nous collectons, comment nous les protégeons et comment vous pouvez les contrôler.

## Données que nous collectons

### Rapports de plantage (Sentry)

L'App utilise [Sentry](https://sentry.io) pour les rapports de plantage automatisés. Lorsque l'App plante ou rencontre une erreur non gérée, les données suivantes **peuvent** être envoyées :

- **Détails de l'erreur** : Type d'exception, message et stack trace
- **Infos sur l'appareil** : Version de l'OS, architecture CPU, utilisation mémoire au moment du plantage
- **Infos sur l'app** : Version de l'app, version du runtime, configuration de build

Les rapports de plantage sont utilisés **uniquement** pour identifier et corriger les bugs. Ils **ne** comprennent **pas** :

- Noms de fichiers, noms de dossiers ou contenu des fichiers
- Informations de compte utilisateur
- Historique de navigation ou chemins de navigation
- Toute information personnellement identifiable (PII)

### Protections de la vie privée dans les rapports de plantage

Avant qu'un rapport de plantage ne soit envoyé, plusieurs couches de nettoyage PII sont automatiquement appliquées :

- **Masquage du nom d'utilisateur** — Les chemins de dossier utilisateur Windows (`C:\Users\<votre-nom-utilisateur>\...`) sont détectés et la partie nom d'utilisateur est remplacée avant la transmission. Cela s'applique également aux chemins UNC (`\\serveur\partage\Users\<nom-utilisateur>\...`).
- **`SendDefaultPii = false`** — La collecte automatique d'adresses IP, de noms de serveurs et d'identifiants utilisateur par le SDK Sentry est entièrement désactivée.
- **Pas de contenu de fichiers** — Les stack traces ne contiennent jamais de contenu de fichiers ou de dossiers ; uniquement des numéros de ligne et des noms de méthodes.

Vous pouvez vérifier l'implémentation vous-même dans le code open source :
[`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

### Paramètres locaux

L'App stocke les préférences utilisateur (thème, langue, dossiers récents, favoris, couleur d'accent personnalisée, etc.) localement sur votre appareil à l'aide de `ApplicationData.LocalSettings` de Windows. Ces données **ne sont jamais** transmises à un serveur.

## Données que nous NE collectons PAS

- Pas d'informations personnelles (nom, e-mail, adresse)
- Pas de contenu du système de fichiers ou de métadonnées de fichiers
- Pas d'analyse d'utilisation ou de télémétrie
- Pas de données de localisation
- Pas d'identifiants publicitaires
- Pas de données partagées avec des tiers à des fins de marketing

## Accès réseau

L'App nécessite un accès Internet uniquement pour :

- **Rapports de plantage** (Sentry) — rapports d'erreur automatiques, peuvent être désactivés (voir « Comment se désinscrire » ci-dessous)
- **Connexions FTP / FTPS / SFTP** — uniquement lorsque l'utilisateur les configure explicitement
- **Restauration de paquets NuGet** — uniquement pendant les builds de développement (ne s'exécute pas pour les utilisateurs finaux)

## Comment se désinscrire des rapports de plantage

Les rapports de plantage peuvent être désactivés directement depuis l'App sans se déconnecter d'Internet :

1. Ouvrez **Paramètres** (en bas à gauche de la barre latérale)
2. Naviguez vers **Avancé**
3. Désactivez **Rapports de plantage**

Le changement prend effet immédiatement. Après désinscription, aucun rapport de plantage ne sera envoyé en aucune circonstance. Les rapports passés déjà sur les serveurs de Sentry expireront toujours selon le calendrier de rétention standard de 90 jours.

## Stockage et conservation des données

- **Serveurs Sentry** : Les rapports de plantage sont stockés dans le centre de données de **Francfort, Allemagne (UE)** de Sentry — choisi pour la conformité RGPD. Les rapports sont automatiquement supprimés après **90 jours**.
- **Paramètres locaux** : Stockés uniquement sur votre appareil. Supprimés lors de la désinstallation de l'App.

## Sentry en tant que sous-traitant (RGPD)

Sentry agit en tant que Sous-traitant (Data Processor) pour les rapports de plantage sous le RGPD. Pour les détails sur les pratiques de confidentialité et les mesures de sécurité de Sentry :

- **Politique de confidentialité de Sentry** : [https://sentry.io/privacy/](https://sentry.io/privacy/)
- **Sécurité de Sentry** : [https://sentry.io/security/](https://sentry.io/security/)
- **RGPD de Sentry** : [https://sentry.io/legal/dpa/](https://sentry.io/legal/dpa/)

LumiBear Studio a examiné les conditions de traitement des données de Sentry et a sélectionné la région UE (`o4510949994266624.ingest.de.sentry.io`) pour garantir que les données de plantage ne quittent pas l'Espace économique européen sans garanties appropriées.

## Confidentialité des enfants

L'App ne collecte pas sciemment de données d'enfants de moins de 13 ans. L'App ne cible pas les enfants et ne collecte aucune information personnelle qui pourrait identifier un enfant.

## Vos droits

Étant donné que nous ne collectons pas de données personnelles, il n'y a pas de données personnelles à consulter, modifier ou supprimer. Plus précisément :

- **Droit d'accès / portabilité** : Non applicable — aucune donnée personnelle détenue par nous.
- **Droit de suppression** : Non applicable — aucune donnée personnelle détenue par nous. Les paramètres locaux peuvent être supprimés en désinstallant l'App.
- **Droit de désinscription** : Disponible dans Paramètres > Avancé > Rapports de plantage (voir « Comment se désinscrire » ci-dessus).

## Open Source

LumiFinder est open source sous la licence GPL v3.0. Vous êtes le bienvenu pour inspecter, auditer ou modifier le code vous-même :

- **Code source** : [https://github.com/LumiBearStudio/LumiFinder](https://github.com/LumiBearStudio/LumiFinder)
- **Bibliothèques open source utilisées** : Voir [OpenSourceLicenses.md](../OpenSourceLicenses.md)

## Contact

Si vous avez des questions sur cette politique de confidentialité, avez trouvé une violation ou souhaitez exercer les droits décrits ci-dessus :

- **Issues GitHub** : [https://github.com/LumiBearStudio/LumiFinder/issues](https://github.com/LumiBearStudio/LumiFinder/issues)
- **Divulgation de sécurité** : Voir [SECURITY.md](../SECURITY.md)

## Modifications de cette politique

Nous pouvons mettre à jour cette politique de temps en temps à mesure que l'App évolue ou que les exigences légales changent. Les changements importants seront annoncés via GitHub Releases. Chaque mise à jour incrémente la date « Dernière mise à jour » en haut de ce document. L'historique des versions est disponible en permanence dans l'[historique Git](https://github.com/LumiBearStudio/LumiFinder/commits/main/PRIVACY.md).
