# ⚔️ Endless Pull

[Español](#-español) | [English](#-english) | [日本語](#-日本語)

---

## 🇪🇸 Español

**Endless Pull** es un RPG táctico gacha 2D y simulador de gestión de base inspirado en la estética y mecánicas de obras de fantasía oscura y manhwas tácticos (como *Pick Me Up*). Desarrollado en Unity (C#), el proyecto combina combate en tiempo real por escuadras, toma de decisiones estratégicas, economía persistente y experimentación con Inteligencia Artificial por Refuerzo (Unity ML-Agents).

### 🎮 Jugar la Demo
* Jugar en Itch.io (PC Windows / WebGL) - <https://sergiog-7.itch.io/endless-pull> (contraseña: level5)
* Ver gameplay en YouTube - <https://youtu.be/placeholder-gameplay>
* Ver combate y ML-Agents Gym en YouTube - <https://youtu.be/placeholder-gym>

### 🛠️ Tecnologías y Herramientas
* **Motor:** Unity (C#), uGUI, TextMeshPro
* **Plataformas:** Android, PC (Windows), WebGL
* **IA & Simulación:** Unity ML-Agents (Reinforcement Learning), Finite State Machines (FSM)
* **Patrones:** Arquitectura Data-Driven (ScriptableObjects), State Machine (FSM), Observer / Event-Driven, Object Pooling, Persistencia JSON.

### 🚀 Desafíos Técnicos
* **Arquitectura RPG Compleja y Simulación de Base Data-Driven:** Sistema escalable de 100 héroes únicos y 29 subclases especializadas a partir de 3★ sobre ScriptableObjects. Simulación autónoma de base con asignación de trabajadores a instalaciones (Granja, Cantina, Taller, Campo de Entrenamiento), desgaste y forja de equipamiento, sinergias de escuadra y jerarquía social.
* **Combate Táctico en Tiempo Real y Gimnasio ML-Agents:** Combate por escuadras con gestión de amenaza (*aggro*) limitada en tanques (máximo 2 enemigos para permitir flanqueos a la retaguardia), IA de curación prioritaria por porcentaje de salud y avisos de peligro en suelo (*telegraphing* de jefes). Incluye una arena desacoplada (`Gym_Combat`) con simulación acelerada ($10\times$) y vector de 20 observaciones normalizadas para entrenamiento por refuerzo.
* **Localización CJK Dinámica y UI Ergonómica Multiplataforma:** Gestor trilingüe (Español, Inglés y Japonés) con cambio en caliente sin recargar paneles, fuentes CJK dinámicas mediante *Fallback Font Assets* en TextMeshPro para evitar caracteres rotos (*tofu*), y navegación modal en pila optimizada para ratón y pantallas táctiles (sin dependencias de teclado ni hover).

---

## 🇬🇧 English

**Endless Pull** is a 2D tactical gacha RPG and fortress management simulator inspired by dark fantasy tactical manhwas (such as *Pick Me Up*). Developed in Unity (C#), the project combines real-time role-based squad combat, strategic resource management, persistent economy, and Reinforcement Learning AI experimentation (Unity ML-Agents).

### 🎮 Play the Demo
* Play on Itch.io (PC Windows / WebGL) - <https://sergiog-7.itch.io/endless-pull> (password: level5)
* Watch gameplay on YouTube - <https://youtu.be/placeholder-gameplay>
* Watch combat & ML-Agents Gym on YouTube - <https://youtu.be/placeholder-gym>

### 🛠️ Technologies & Tools
* **Engine:** Unity (C#), uGUI, TextMeshPro
* **Platforms:** Android, PC (Windows), WebGL
* **AI & Simulation:** Unity ML-Agents (Reinforcement Learning), Finite State Machines (FSM)
* **Patterns:** Data-Driven Architecture (ScriptableObjects), State Machine (FSM), Observer / Event-Driven, Object Pooling, JSON Persistence.

### 🚀 Technical Challenges
* **Complex RPG Architecture & Data-Driven Base Simulation:** Scalable system featuring 100 unique heroes and 29 specialized subclasses from 3★ upward built on ScriptableObjects. Autonomous base simulation with worker assignment across facilities (Farm, Canteen, Workshop, Training Grounds), equipment durability/crafting, squad synergies, and social hierarchy.
* **Real-Time Tactical Combat & ML-Agents Gym:** Squad combat system featuring capped tank threat (*aggro*) management (maximum 2 enemies to allow tactical backline flanking), health-percentage priority healer AI, and ground area telegraph warnings. Features a decoupled training arena (`Gym_Combat`) running at $10\times$ timescale with a 20-observation normalized vector for reinforcement learning agents.
* **Dynamic CJK Localization & Ergonomic Cross-Platform UI:** Hot-swappable trilingual support (Spanish, English, Japanese) without UI reloads, dynamic CJK fonts using TextMeshPro Fallback Font Assets to eliminate missing glyphs (*tofu*), and stack-based modal navigation tailored for both mouse and touch screens (no keyboard or hover-only dependencies).

---

## 🇯🇵 日本語

**Endless Pull（エンドレス・プル）**は、ダークファンタジーや戦術系マンガ（『Pick Me Up』等）の世界観に着想を得た、2DタクティカルガチャRPGおよび拠点経営シミュレーションゲームです。リアルタイムの役割ベース部隊戦闘、戦略的なリソース管理、永続的な経済システム、そして強化学習AI（Unity ML-Agents）の実験的導入を統合しています。

### 🎮 デモをプレイする
* Itch.ioでプレイ (PC Windows / WebGL) - <https://sergiog-7.itch.io/endless-pull> (パスワード：level5)
* YouTubeでゲームプレイを見る - <https://youtu.be/placeholder-gameplay>
* YouTubeで戦闘＆ML-Agentsジムを見る - <https://youtu.be/placeholder-gym>

### 🛠️ 使用技術とツール
* **エンジン:** Unity (C#), uGUI, TextMeshPro
* **プラットフォーム:** Android, PC (Windows), WebGL
* **AI・シミュレーション:** Unity ML-Agents（強化学習）、有限ステートマシン (FSM)
* **設計パターン:** データドリブン・アーキテクチャ (ScriptableObjects)、ステートマシン (FSM)、Observer / イベント駆動、オブジェクトプーリング、JSON永続化

### 🚀 技術的課題
* **複雑なRPG設計とデータ駆動型拠点シミュレーション:** ScriptableObjectを活用した100名のユニーク英雄と29種類の上位クラス設計。施設への人員配置（農場、酒場、工房、訓練所）、装備の耐久度・鍛造、部隊シナジー、序列システムを備えた自律的な拠点シミュレーション。
* **リアルタイム戦術戦闘とML-Agents訓練環境:** タンクのヘイト上限管理（後衛への回り込みを可能にする最大2体制限）、残HP比率を優先するヒーラーAI、ボスの攻撃予兆表示（テレグラフ）を実装。20次元の正規化観測ベクトルを備え、10倍速で動作する強化学習用の独立アリーナ（`Gym_Combat`）を構築。
* **動的CJKローカライゼーションと直感的なクロスプラットフォームUI:** パネルの再読み込みなしに切り替え可能な3言語（日・英・西）対応。TextMeshProのフォールバックアセットによる文字化け（トーフ）防止、マウスとタッチ操作の両方に最適化されたスタック型モーダルナビゲーション。
