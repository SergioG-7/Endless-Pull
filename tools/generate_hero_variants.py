# -*- coding: utf-8 -*-
"""Genera variantes de aspecto de UN heroe para poder elegirlas a ojo.

Llama al MISMO compositor que generate_heroes_lpc.py (orden de capas, cabezas, ojos y
rampas de color incluidos) y solo cambia la tabla FIJOS entre pasada y pasada. Sirve para
los heroes que necesitan un aspecto concreto: Loki es el protagonista, no puede salir de
un sorteo.

Escribe fuera de Assets/ a proposito: Unity no reimporta nada hasta que se elija una.

Uso:
    python tools/generate_hero_variants.py --hero Loki --salida <carpeta>
"""

import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import generate_heroes_lpc as G


# Variantes del protagonista: el chaval de la calle que acaba por encima de los 5*. Todas con
# cuerpo juvenil y espada, que es lo que le pega; cambian pelo, color y piel.
VARIANTES = {
    # Loki es el nombre de Maestro de Han Isratte, el protagonista: joven espigado de veinte y
    # pocos, pelo negro con flequillo y ojos marrones. Todas las variantes parten de ahi y solo
    # cambian el corte, que es lo unico que el pack LPC ofrece en varios sabores.
    "Loki": {
        "a_bangs":        {"cuerpo": "teen", "piel": "light", "pelo": "bangs",
                           "color_pelo": "raven", "ojos": "brown", "rol": "sword"},
        "b_parted_side":  {"cuerpo": "teen", "piel": "light", "pelo": "parted_side_bangs",
                           "color_pelo": "raven", "ojos": "brown", "rol": "sword"},
        "c_swoop":        {"cuerpo": "teen", "piel": "light", "pelo": "swoop",
                           "color_pelo": "raven", "ojos": "brown", "rol": "sword"},
        "d_messy1":       {"cuerpo": "teen", "piel": "light", "pelo": "messy1",
                           "color_pelo": "raven", "ojos": "brown", "rol": "sword"},
        "e_parted":       {"cuerpo": "teen", "piel": "light", "pelo": "parted",
                           "color_pelo": "raven", "ojos": "brown", "rol": "sword"},
        "f_unkempt":      {"cuerpo": "teen", "piel": "light", "pelo": "unkempt",
                           "color_pelo": "raven", "ojos": "brown", "rol": "sword"},
    },
}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--hero", required=True, help="heroName tal cual esta en el asset")
    ap.add_argument("--anim", default="walk")
    ap.add_argument("--salida", required=True, help="carpeta destino, fuera de Assets/")
    args = ap.parse_args()

    variantes = VARIANTES.get(args.hero)
    if not variantes:
        print("No hay variantes definidas para " + args.hero, file=sys.stderr)
        return 1

    heroes = [h for h in G.leer_heroes() if h["heroName"] == args.hero]
    if not heroes:
        print("No encuentro el HeroData de " + args.hero, file=sys.stderr)
        return 1

    hero = heroes[0]
    rampas_piel = G.cargar_rampas("body")
    rampas_pelo = G.cargar_rampas("hair")
    rampas_ojos = G.cargar_rampas("eye")
    genero = G.asignar_generos(G.leer_heroes())[args.hero]

    os.makedirs(args.salida, exist_ok=True)
    fallos = 0

    for etiqueta, ajuste in sorted(variantes.items()):
        G.FIJOS[args.hero] = ajuste
        lienzo, error = G.generar(hero, args.anim, genero, rampas_piel, rampas_pelo,
                                  rampas_ojos, {}, verbose=False)
        if lienzo is None:
            print("FALLO %s: %s" % (etiqueta, error), file=sys.stderr)
            fallos += 1
            continue

        destino = os.path.join(args.salida, args.hero.lower() + "_" + etiqueta + ".png")
        lienzo.save(destino)
        print("ok " + destino)

    G.FIJOS.pop(args.hero, None)
    return 0 if not fallos else 2


if __name__ == "__main__":
    sys.exit(main())
