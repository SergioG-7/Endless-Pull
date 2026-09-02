# ⚔️ Endless Pull

[Español](#-español) | [English](#-english) | [日本語](#-日本語)

---

## 🇪🇸 Español

**Endless Pull** es un RPG táctico gacha 2D y simulador de gestión de base inspirado en la estética y mecánicas de obras de fantasía oscura y manhwas tácticos (como *Pick Me Up*). Desarrollado en Unity (C#), el proyecto combina combate en tiempo real por escuadras, toma de decisiones estratégicas, economía persistente y experimentación con Inteligencia Artificial por Refuerzo (Unity ML-Agents).

### 🎮 Jugar la Demo
* Jugar en Itch.io (PC Windows / WebGL) - [https://sergiog-7.itch.io/endless-pull] (contraseña: level5)
* Ver gameplay en YouTube - [https://youtu.be/placeholder-gameplay]
* Ver combate y ML-Agents Gym en YouTube - [https://youtu.be/placeholder-gym]

### 🧩 Características Principales
* **Torre y Combate con IA Autónoma:** Bucle de escalada por pisos con combate en tiempo real por escuadras, gestión de amenaza (*aggro*) limitada en tanques, IA de curación prioritaria por porcentaje de salud, estados alterados y telegraphing de ataques de jefe.
* **Gestión de Campamento:** Simulación autónoma de base con asignación de trabajadores a instalaciones — Granja, Cantina, Taller de Alquimia, Santuario de Ascensión, Sala de Guerra y más — con desgaste y forja de equipamiento, sinergias de escuadra y jerarquía social.
* **Soporte Trilingüe Dinámico:** Español, English y 日本語 conmutables en caliente sin reiniciar ni reabrir paneles, con fuentes CJK dinámicas (*Fallback Font Assets*) para japonés sin caracteres rotos (*tofu*).
* **Controles Unificados:** Input táctil/ratón en toda la interfaz (sin dependencias de teclado ni hover-only) y navegación modal en pila (Roster → Ficha de Héroe → Selector de Equipamiento) sin ciclos ni referencias rotas.
* **Sistema de Audio Modular:** Tres canales independientes (Música, SFX de Combate, SFX de Interfaz) sincronizados entre el menú principal y el menú de pausa, persistentes vía `PlayerPrefs`.
* **Arquitectura Data-Driven y Gimnasio ML-Agents:** 39 héroes únicos y 18 subclases especializadas a partir de 3★ sobre ScriptableObjects; arena desacoplada (`Gym_Combat`) con simulación acelerada (10×) y vector de 8 observaciones normalizadas para entrenamiento por refuerzo.

### 🛠️ Tecnologías y Herramientas
* **Motor Core:** Unity 6.5 (6000.5.8f1) — C#, uGUI, TextMeshPro
* **IA & Simulación:** Unity ML-Agents (Reinforcement Learning), Finite State Machines (FSM)
* **Patrones:** Arquitectura Data-Driven (ScriptableObjects), State Machine (FSM), Observer / Event-Driven, Object Pooling, Persistencia JSON.

### 🖥️ Plataformas Objetivo
* **Standalone PC:** Windows (build principal); Mac/Linux vía el mismo pipeline de Unity.
* **Android:** APK.
* **Itch.io:** WebGL y build de PC descargable.

---

## 🇬🇧 English

**Endless Pull** is a 2D tactical gacha RPG and fortress management simulator inspired by dark fantasy tactical manhwas (such as *Pick Me Up*). Developed in Unity (C#), the project combines real-time role-based squad combat, strategic resource management, persistent economy, and Reinforcement Learning AI experimentation (Unity ML-Agents).

### 🎮 Play the Demo
* Play on Itch.io (PC Windows / WebGL) - [https://sergiog-7.itch.io/endless-pull] (password: level5)
* Watch gameplay on YouTube - [https://youtu.be/placeholder-gameplay]
* Watch combat & ML-Agents Gym on YouTube - [https://youtu.be/placeholder-gym]

### 🧩 Key Features
* **Tower Climb & Autonomous Combat AI:** Floor-by-floor climbing loop with real-time squad combat, capped tank threat (*aggro*) management, health-percentage priority healer AI, status effects, and boss attack telegraphing.
* **Camp Management:** Autonomous base simulation with worker assignment across facilities — Farm, Canteen, Alchemy Workshop, Ascension Sanctuary, War Room and more — with equipment durability/crafting, squad synergies, and social hierarchy.
* **Dynamic Trilingual Support:** Spanish, English, and 日本語, hot-swappable at runtime without reloading or reopening panels, with dynamic CJK fonts (*Fallback Font Assets*) for glyph-perfect Japanese.
* **Unified Controls:** Touch/mouse input across the entire UI (no keyboard or hover-only dependencies) and stack-based modal navigation (Roster → Hero Card → Equipment Selector) with no cycles or broken references.
* **Modular Audio System:** Three independent channels (Music, Combat SFX, UI SFX) synchronized between the main menu and the pause menu, persisted via `PlayerPrefs`.
* **Data-Driven Architecture & ML-Agents Gym:** 39 unique heroes and 18 specialized subclasses from 3★ upward built on ScriptableObjects; a standalone training arena (`Gym_Combat`) running at 10× timescale with an 8-observation normalized vector for reinforcement learning.

### 🛠️ Technologies & Tools
* **Core Engine:** Unity 6.5 (6000.5.8f1) — C#, uGUI, TextMeshPro
* **AI & Simulation:** Unity ML-Agents (Reinforcement Learning), Finite State Machines (FSM)
* **Patterns:** Data-Driven Architecture (ScriptableObjects), State Machine (FSM), Observer / Event-Driven, Object Pooling, JSON Persistence.

### 🖥️ Target Platforms
* **Standalone PC:** Windows (primary build); Mac/Linux via the same Unity pipeline.
* **Android:** APK.
* **Itch.io:** WebGL and downloadable PC build.

---

## 🇯🇵 日本語

**Endless Pull（エンドレス・プル）**は、ダークファンタジーや戦術系マンガ（『Pick Me Up』等）の世界観に着想を得た、2DタクティカルガチャRPGおよび拠点経営シミュレーションゲームです。Unity（C#）で開発され、リアルタイムの役割ベース部隊戦闘、戦略的なリソース管理、永続的な経済システム、そして強化学習AI（Unity ML-Agents）の実験的導入を統合しています。

### 🎮 デモをプレイする
* Itch.ioでプレイ (PC Windows / WebGL) - [https://sergiog-7.itch.io/endless-pull] (パスワード：level5)
* YouTubeでゲームプレイを見る - [https://youtu.be/placeholder-gameplay]
* YouTubeで戦闘＆ML-Agentsジムを見る - [https://youtu.be/placeholder-gym]

### 🧩 主な特徴
* **塔の攻略と自律戦闘AI:** リアルタイム部隊戦闘によるフロア攻略ループ。タンクのヘイト上限管理、残HP比率を優先するヒーラーAI、状態異常、ボス攻撃の予兆表示を実装。
* **拠点経営:** 農場・酒場・錬金工房・昇格の聖域・作戦室などの施設への人員配置による自律的な拠点シミュレーション。装備の耐久度・鍛造、部隊シナジー、序列システムを搭載。
* **動的3言語対応:** スペイン語・英語・日本語をリロードやパネルの再表示なしにリアルタイムで切り替え可能。動的CJKフォント（フォールバックアセット）により文字化け（トーフ）のない日本語表示。
* **統一された操作系:** キーボードやホバー操作に依存しない、UI全体でのタッチ／マウス入力。Roster → 英雄カード → 装備選択のスタック型モーダルナビゲーションは循環や参照切れなし。
* **モジュール式オーディオシステム:** 音楽・戦闘SFX・UI SFXの独立した3チャンネルをメインメニューとポーズメニューで同期。`PlayerPrefs`で永続化。
* **データドリブン設計とML-Agentsジム:** ScriptableObjectベースの39名のユニーク英雄と★3から分岐する18種類の専門クラス。10倍速で動作する独立訓練アリーナ（`Gym_Combat`）と8次元正規化観測ベクトルによる強化学習。

### 🛠️ 使用技術とツール
* **コアエンジン:** Unity 6.5 (6000.5.8f1) — C#, uGUI, TextMeshPro
* **AI・シミュレーション:** Unity ML-Agents（強化学習）、有限ステートマシン (FSM)
* **設計パターン:** データドリブン・アーキテクチャ (ScriptableObjects)、ステートマシン (FSM)、Observer / イベント駆動、オブジェクトプーリング、JSON永続化

### 🖥️ 対象プラットフォーム
* **スタンドアロンPC:** Windows（主要ビルド）。同じUnityパイプラインでMac/Linuxにも対応。
* **Android:** APK。
* **Itch.io:** WebGLおよびダウンロード可能なPCビルド。
