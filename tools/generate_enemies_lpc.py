# -*- coding: utf-8 -*-
"""Genera un spritesheet LPC uniforme para cada uno de los 6 tipos de enemigo de
Endless Pull. Comparte el formato de Assets/_EndlessPull/ScriptableObjects/Enemy*.asset
(9 columnas x 4 filas, 64x64) para que EnemyController los recorte igual que a los
héroes (ver LPCAnimator.SliceWalkSheet).

A diferencia de generate_heroes_lpc.py, aquí no hay 50 individuos con estadísticas
propias: hay 6 tipos fijos, así que cada uno tiene una receta de capas explícita en
vez de un sorteo por nombre. El resultado es igual de determinista: mismo tipo,
mismo PNG, siempre.

Uso:
    python tools/generate_enemies_lpc.py [--anim walk] [--dry-run]
"""

import argparse
import json
import os
import sys

from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LPC = os.path.join(RAIZ, "tools", "LPC_Assets")
HOJAS = os.path.join(LPC, "spritesheets")
PALETAS = os.path.join(LPC, "palette_definitions")
SALIDA = os.path.join(RAIZ, "Assets", "_EndlessPull", "Sprites", "Enemies")

LIENZO = (576, 256)

# Una receta por tipo: cuerpo, cabeza propia (con su animación ya en el nombre de
# carpeta), tono de piel (None si el cuerpo no se recolorea, como el esqueleto),
# torso, categoría de arma y, si aplica, familia de capa.
RECETAS = {
    "Enemy_Goblin_Test": dict(
        cuerpo="teen", cabeza="goblin/adult", piel="green",
        torso="torso/clothes/longsleeve", arma="blunt", capa=None),
    "Enemy_GoblinArcher": dict(
        cuerpo="teen", cabeza="goblin/adult", piel="pale_green",
        torso="torso/clothes/longsleeve", arma="ranged", capa=None),
    "Enemy_OrcBrawler": dict(
        cuerpo="muscular", cabeza="orc/male", piel="dark_green",
        torso="torso/armour/leather", arma="blunt", capa=None),
    "Enemy_SkeletonRogue": dict(
        cuerpo="skeleton", cabeza="skeleton/adult", piel=None,
        torso=None, arma="sword", capa=None),
    "Enemy_DarkShaman": dict(
        cuerpo="muscular", cabeza="orc/male", piel="green",
        torso="torso/clothes/robe", arma="magic", capa="cape/tattered"),
    "Enemy_GoblinKing": dict(
        cuerpo="muscular", cabeza="goblin/adult", piel="bright_green",
        torso="torso/armour/plate", arma="blunt", capa="cape/solid"),
}


# --------------------------------------------------------------------------
# Localización de capas (subconjunto de generate_heroes_lpc.py: aquí no hacen
# falta escudos ni peinados, los enemigos no llevan ninguno de los dos)
# --------------------------------------------------------------------------

def hojas_con(base, anim):
    encontradas = []
    raiz = os.path.join(HOJAS, base)
    if not os.path.isdir(raiz):
        return encontradas

    objetivo = anim + ".png"
    for carpeta, _, ficheros in os.walk(raiz):
        if objetivo in ficheros:
            encontradas.append(os.path.join(carpeta, objetivo))

    return sorted(encontradas)


def mide(ruta):
    try:
        with Image.open(ruta) as im:
            return im.size
    except Exception:
        return None


DETRAS = ("universal_behind", "background", "behind", "bg")


def armas_con(categoria, anim):
    salida = []
    raiz = os.path.join(HOJAS, "weapon", categoria)
    if not os.path.isdir(raiz):
        return salida

    for carpeta, _, ficheros in os.walk(raiz):
        if os.path.basename(carpeta) != anim:
            continue

        partes = carpeta.replace("/", os.sep).split(os.sep)
        if any(x in DETRAS for x in partes):
            continue

        for fichero in sorted(ficheros):
            if not fichero.endswith(".png"):
                continue

            delante = os.path.join(carpeta, fichero)
            if mide(delante) != LIENZO:
                continue

            salida.append((delante, trasera(delante, anim, fichero)))

    return sorted(salida)


def trasera(delante, anim, fichero):
    base = os.path.dirname(os.path.dirname(delante))

    for marca in DETRAS:
        candidato = os.path.join(base, marca, anim, fichero)
        if os.path.isfile(candidato) and mide(candidato) == LIENZO:
            return candidato

        candidato = os.path.join(base, anim, marca, fichero)
        if os.path.isfile(candidato) and mide(candidato) == LIENZO:
            return candidato

    return None


def cuerpo(tipo, anim):
    return os.path.join(HOJAS, "body", "bodies", tipo, anim + ".png")


def cabeza(familia, anim):
    return os.path.join(HOJAS, "head", "heads", familia, anim + ".png")


# --------------------------------------------------------------------------
# Recoloreado por paleta (idéntico a generate_heroes_lpc.py)
# --------------------------------------------------------------------------

def cargar_rampas(familia):
    ruta = os.path.join(PALETAS, familia, familia + "_ulpc.json")
    if not os.path.isfile(ruta):
        return {}

    with open(ruta, encoding="utf-8") as f:
        return json.load(f)


def a_rgb(hexa):
    hexa = hexa.lstrip("#")
    return tuple(int(hexa[i:i + 2], 16) for i in (0, 2, 4))


def recolorear(imagen, rampa_origen, rampa_destino):
    if not rampa_origen or not rampa_destino:
        return imagen

    pares = min(len(rampa_origen), len(rampa_destino))
    mapa = {a_rgb(rampa_origen[i]): a_rgb(rampa_destino[i]) for i in range(pares)}

    imagen = imagen.convert("RGBA")
    pixeles = imagen.load()
    ancho, alto = imagen.size

    for y in range(alto):
        for x in range(ancho):
            r, g, b, a = pixeles[x, y]
            if a == 0:
                continue

            nuevo = mapa.get((r, g, b))
            if nuevo:
                pixeles[x, y] = (nuevo[0], nuevo[1], nuevo[2], a)

    return imagen


def pegar(lienzo, ruta, rampa=None, rampas=None, base="light"):
    if not ruta or not os.path.isfile(ruta):
        return False

    capa = Image.open(ruta).convert("RGBA")

    if rampa and rampas:
        capa = recolorear(capa, rampas.get(base), rampas.get(rampa))

    if capa.size != lienzo.size:
        return False

    lienzo.alpha_composite(capa)
    return True


# --------------------------------------------------------------------------
# Composición
# --------------------------------------------------------------------------

def generar(nombre, receta, anim, rampas_piel, cache, verbose=True):
    lienzo = Image.new("RGBA", LIENZO, (0, 0, 0, 0))
    capas = []

    capa_base = receta["capa"]
    if capa_base:
        bg = os.path.join(HOJAS, capa_base, "bg", anim + ".png")
        if pegar(lienzo, bg):
            capas.append("cloak_bg")

    categoria = receta["arma"]
    armas = cache.setdefault(("weapon", categoria), armas_con(categoria, anim))
    arma = armas[0] if armas else None

    if arma and arma[1] and pegar(lienzo, arma[1]):
        capas.append("weapon_behind")

    piel = receta["piel"]
    tipo = receta["cuerpo"]

    if not pegar(lienzo, cuerpo(tipo, anim), piel, rampas_piel, "light"):
        return None, f"sin cuerpo ({tipo})"
    capas.append("body:" + tipo)

    if not pegar(lienzo, cabeza(receta["cabeza"], anim), piel, rampas_piel, "light"):
        return None, f"sin cabeza ({receta['cabeza']})"
    capas.append("head:" + receta["cabeza"])

    torso_base = receta["torso"]
    if torso_base:
        torsos = cache.setdefault(("torso", torso_base), hojas_con(torso_base, anim))
        torso = torsos[0] if torsos else None
        if pegar(lienzo, torso):
            capas.append("torso")

    if capa_base:
        fg = os.path.join(HOJAS, capa_base, "fg", anim + ".png")
        if pegar(lienzo, fg):
            capas.append("cloak_fg")

    if arma and pegar(lienzo, arma[0]):
        capas.append("weapon")

    if verbose:
        print(f"  {nombre:<20} -> {', '.join(capas)}")

    return lienzo, None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--anim", default="walk", help="animacion LPC a componer (walk por defecto)")
    ap.add_argument("--dry-run", action="store_true", help="no escribe ningun PNG")
    args = ap.parse_args()

    if not os.path.isdir(HOJAS):
        print("No esta tools/LPC_Assets. Clona el repositorio del generador antes.", file=sys.stderr)
        return 1

    rampas_piel = cargar_rampas("body")

    if not args.dry_run:
        os.makedirs(SALIDA, exist_ok=True)

    print(f"Generando {len(RECETAS)} spritesheet(s) de enemigo con la animacion '{args.anim}':")

    cache = {}
    hechos = 0
    fallos = []

    for nombre, receta in RECETAS.items():
        lienzo, error = generar(nombre, receta, args.anim, rampas_piel, cache)
        if lienzo is None:
            fallos.append((nombre, error))
            continue

        if not args.dry_run:
            lienzo.save(os.path.join(SALIDA, nombre + ".png"))

        hechos += 1

    print(f"\n{hechos} spritesheet(s) generado(s) en {SALIDA}")
    for nombre, error in fallos:
        print(f"  FALLO {nombre}: {error}", file=sys.stderr)

    return 0 if not fallos else 2


if __name__ == "__main__":
    sys.exit(main())
