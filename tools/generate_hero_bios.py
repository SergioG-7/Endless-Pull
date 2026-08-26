# -*- coding: utf-8 -*-
"""Genera y escribe bios cortas (ES/EN/JA) en los 50 Hero_*.asset del catalogo.

Reutiliza el mismo parser y la misma heuristica de rol que generate_heroes_lpc.py
(arquetipo por defensa/ataque/velocidad, seedeada por el nombre del heroe) para que
el texto de la bio coincida con el arquetipo visual ya asignado a cada heroe.

No genera texto libre por heroe: combina plantillas fijas por rareza (1-5 estrellas)
y por rol (Tanque/Melee/Rango), elegidas de forma deterministica con el nombre como
semilla, igual que hace el generador de sprites.

Uso:
    python tools/generate_hero_bios.py [--dry-run]
"""

import argparse
import os
import random
import re

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HEROES = os.path.join(RAIZ, "Assets", "_EndlessPull", "ScriptableObjects", "Heroes")

CAMPOS = ("heroName", "starRank", "baseAttack", "baseDefense", "moveSpeed")


def leer_heroes():
    heroes = []
    for nombre in sorted(os.listdir(HEROES)):
        if not nombre.endswith(".asset"):
            continue

        datos = {"asset": nombre}
        with open(os.path.join(HEROES, nombre), encoding="utf-8") as f:
            for linea in f:
                m = re.match(r"\s*(\w+):\s*(.+?)\s*$", linea)
                if not m:
                    continue

                clave, valor = m.group(1), m.group(2)
                if clave not in CAMPOS:
                    continue

                if clave == "heroName":
                    datos[clave] = valor
                elif clave == "moveSpeed":
                    datos[clave] = float(valor)
                else:
                    datos[clave] = int(float(valor))

        if "heroName" in datos:
            datos.setdefault("starRank", 1)
            heroes.append(datos)

    return heroes


def elegir(rng, lista):
    return lista[rng.randrange(len(lista))] if lista else None


def arquetipo(hero):
    """Misma heuristica que generate_heroes_lpc.arquetipo(): reproducida aqui para no
    depender de PIL (que ese script importa solo para componer sprites)."""
    defensa = hero.get("baseDefense", 0)
    ataque = max(1, hero.get("baseAttack", 1))
    velocidad = hero.get("moveSpeed", 2.5)

    proporcion = defensa / ataque
    rng = random.Random("rol:" + hero["heroName"])

    if proporcion >= 0.42:
        return "shield"

    if velocidad >= 2.75 and proporcion <= 0.28:
        return elegir(rng, ["bow", "bow", "staff"])

    return elegir(rng, ["sword", "sword", "spear", "mace"])


ROLE_BUCKET = {
    "shield": "tank",
    "bow": "ranged",
    "staff": "ranged",
    "sword": "melee",
    "spear": "melee",
    "mace": "melee",
}

# Apertura por rareza (1..5 estrellas), una frase por idioma, sin adjetivos con genero
# que puedan chocar con el nombre del heroe.
TIER_ES = [
    "{n} acaba de sumarse a las filas de la torre, con más ganas que experiencia.",
    "{n} ha superado ya varios combates y empieza a ganarse un nombre en la torre.",
    "{n} carga con más batallas de las que se pueden contar con los dedos de una mano.",
    "El nombre de {n} circula entre las tropas como sinónimo de victoria.",
    "Pocos en la torre no han oído hablar de {n}, cuyas hazañas ya son leyenda.",
]

TIER_EN = [
    "{n} has just joined the tower's ranks, more eager than experienced.",
    "{n} has already come through several fights and is starting to make a name in the tower.",
    "{n} carries more battles than can be counted on one hand.",
    "{n}'s name circulates among the troops as a byword for victory.",
    "Few in the tower haven't heard of {n}, whose feats are already legend.",
]

TIER_JA = [
    "{n}は塔に加わったばかりの新兵で、経験より意気込みが先立つ。",
    "{n}はすでにいくつもの戦いをくぐり抜け、塔の中で名を上げ始めている。",
    "{n}は数えきれないほどの戦いを重ねてきた古参兵だ。",
    "{n}の名は勝利の代名詞として兵士たちの間で語られている。",
    "塔の中で{n}の名を知らぬ者は少なく、その武勲はすでに伝説となっている。",
]

# Frase de rol, 3 variantes por rol/idioma para dar variedad sin escribir texto libre.
ROLE_ES = {
    "tank": [
        "Se planta en primera línea y absorbe los golpes que el resto no podría soportar.",
        "No retrocede ni un paso: es el muro que protege a toda la escuadra.",
        "Aguanta el frente de batalla mientras sus compañeros golpean desde atrás.",
    ],
    "melee": [
        "Combate cuerpo a cuerpo sin dudar, buscando siempre el punto débil del enemigo.",
        "Entra en combate directo, golpe tras golpe, hasta que uno de los dos cae.",
        "Prefiere el choque frontal: acercarse, golpear y no dejar que el rival respire.",
    ],
    "ranged": [
        "Golpea desde la distancia, castigando al enemigo antes de que pueda acercarse.",
        "Mantiene las distancias y dispara con precisión mientras otros cubren el frente.",
        "Prefiere herir desde lejos, eligiendo el momento exacto para atacar.",
    ],
}

ROLE_EN = {
    "tank": [
        "Holds the front line and soaks up the hits the rest of the squad could not survive.",
        "Never gives an inch: the wall that shields the whole squad.",
        "Holds the battle line while allies strike from behind.",
    ],
    "melee": [
        "Fights hand to hand without hesitation, always hunting the enemy's weak point.",
        "Closes into direct combat, blow after blow, until one side falls.",
        "Prefers the head-on clash: close in, strike, and give the enemy no room to breathe.",
    ],
    "ranged": [
        "Strikes from a distance, punishing the enemy before they can close in.",
        "Keeps their distance and fires with precision while others hold the line.",
        "Prefers to wound from afar, choosing the exact moment to strike.",
    ],
}

ROLE_JA = {
    "tank": [
        "前線に立ち、他の誰も耐えられない攻撃を一身に受け止める。",
        "一歩も引かず、部隊全体を守る盾となる。",
        "前線を支え、仲間が背後から攻撃できるようにする。",
    ],
    "melee": [
        "迷わず接近戦を挑み、常に敵の弱点を探る。",
        "一撃また一撃と直接ぶつかり合い、どちらかが倒れるまで戦う。",
        "正面からの激突を好み、近づいて叩き、敵に息をつかせない。",
    ],
    "ranged": [
        "遠距離から攻撃し、敵が近づく前に打ち倒す。",
        "距離を保ちながら、味方が前線を支える間に正確に狙い撃つ。",
        "遠くから傷を負わせることを好み、攻撃の瞬間を見極める。",
    ],
}


def construir_bio(hero, rol):
    tier = max(1, min(5, hero.get("starRank", 1))) - 1
    rng = random.Random("bio:" + hero["heroName"])

    variante_es = elegir(rng, ROLE_ES[rol])
    variante_en = elegir(rng, ROLE_EN[rol])
    variante_ja = elegir(rng, ROLE_JA[rol])

    nombre = hero["heroName"]
    es = f"{TIER_ES[tier].format(n=nombre)} {variante_es}"
    en = f"{TIER_EN[tier].format(n=nombre)} {variante_en}"
    ja = f"{TIER_JA[tier].format(n=nombre)}{variante_ja}"
    return es, en, ja


def yaml_string(valor):
    escapado = valor.replace("\\", "\\\\").replace('"', '\\"')
    return f'"{escapado}"'


def escribir_bio(ruta, es, en, ja, dry_run):
    with open(ruta, encoding="utf-8") as f:
        lineas = f.readlines()

    salida = []
    en_escrito = False
    ja_escrito = False

    for linea in lineas:
        if re.match(r"\s*bio:\s*", linea):
            salida.append(f"  bio: {yaml_string(es)}\n")
            continue

        if re.match(r"\s*bioEn:\s*", linea):
            salida.append(f"  bioEn: {yaml_string(en)}\n")
            en_escrito = True
            continue

        if re.match(r"\s*bioJa:\s*", linea):
            salida.append(f"  bioJa: {yaml_string(ja)}\n")
            ja_escrito = True
            continue

        salida.append(linea)

        if re.match(r"\s*bio:\s*", linea):
            pass

    # Si el .asset todavia no tiene bioEn/bioJa (campos nuevos), se insertan justo
    # despues de la linea de bio para que Unity los reordene solo al re-serializar.
    if not en_escrito or not ja_escrito:
        resultado = []
        for linea in salida:
            resultado.append(linea)
            if re.match(r"\s*bio:\s*", linea):
                if not en_escrito:
                    resultado.append(f"  bioEn: {yaml_string(en)}\n")
                if not ja_escrito:
                    resultado.append(f"  bioJa: {yaml_string(ja)}\n")
        salida = resultado

    if dry_run:
        return

    with open(ruta, "w", encoding="utf-8", newline="\n") as f:
        f.writelines(salida)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    heroes = leer_heroes()
    print(f"{len(heroes)} heroes encontrados.")

    for hero in heroes:
        rol = ROLE_BUCKET[arquetipo(hero)]
        es, en, ja = construir_bio(hero, rol)
        ruta = os.path.join(HEROES, hero["asset"])
        escribir_bio(ruta, es, en, ja, args.dry_run)
        print(f"[{rol:6}] {hero['heroName']}: {es}")

    if args.dry_run:
        print("Dry-run: no se escribió nada.")


if __name__ == "__main__":
    main()
