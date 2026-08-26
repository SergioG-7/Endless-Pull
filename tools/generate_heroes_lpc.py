# -*- coding: utf-8 -*-
"""Genera un spritesheet LPC por cada heroe del catalogo de Endless Pull.

Compone las capas del Universal LPC Spritesheet Character Generator y exporta un PNG
por heroe a Assets/_EndlessPull/Sprites/Heroes/.

OJO: en LPC el cuerpo base NO trae cabeza. Sin las capas de cabeza, ojos y nariz los
heroes salen decapitados, con el pelo flotando sobre el cuello. El orden real es:

    capa(bg) -> arma(detras) -> escudo(detras) -> cuerpo -> cabeza -> nariz -> ojos
    -> torso -> pelo -> capa(fg) -> arma -> escudo

La cabeza se tine con la MISMA rampa de piel que el cuerpo o no pegan entre si.

La eleccion de cada capa es determinista: sale del nombre del heroe y de sus
estadisticas, asi que volver a lanzar el script produce exactamente lo mismo.

Uso:
    python tools/generate_heroes_lpc.py [--anim walk] [--dry-run]
"""

import argparse
import json
import os
import random
import re
import sys

from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LPC = os.path.join(RAIZ, "tools", "LPC_Assets")
HOJAS = os.path.join(LPC, "spritesheets")
PALETAS = os.path.join(LPC, "palette_definitions")
HEROES = os.path.join(RAIZ, "Assets", "_EndlessPull", "ScriptableObjects", "Heroes")
SALIDA = os.path.join(RAIZ, "Assets", "_EndlessPull", "Sprites", "Heroes")

# Los cuerpos base van dibujados en esta rampa; el pelo, en la suya.
BASE_PIEL = "light"
BASE_PELO = "orange"
BASE_OJOS = "blue"

# Formato estandar de las hojas LPC: 9 columnas x 4 filas de 64x64.
LIENZO = (576, 256)


# --------------------------------------------------------------------------
# Lectura del catalogo
# --------------------------------------------------------------------------

CAMPOS = ("heroName", "starRank", "maxHealth", "maxMP", "baseAttack", "baseDefense", "moveSpeed")


def leer_heroes():
    """Saca los campos que interesan de cada Hero_*.asset sin dependencias de YAML."""
    heroes = []

    for nombre in sorted(os.listdir(HEROES)):
        if not nombre.endswith(".asset"):
            continue

        datos = {"asset": nombre[:-6]}
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


def arquetipo(hero):
    """Deduce el rol del heroe de sus cifras. HeroData no tiene campo de clase y maxMP
    vale 50 en todo el catalogo, asi que las senales utiles son defensa y velocidad;
    dentro de cada familia el reparto lo decide el nombre, que es estable."""
    defensa = hero.get("baseDefense", 0)
    ataque = max(1, hero.get("baseAttack", 1))
    velocidad = hero.get("moveSpeed", 2.5)

    proporcion = defensa / ataque
    rng = random.Random("rol:" + hero["heroName"])

    # Aguantar casi tanto como se pega es de tanque: escudo y nada mas que discutir.
    if proporcion >= 0.42:
        return "shield"

    # Rapidos y de papel: pelean a distancia, con arco o con baculo.
    if velocidad >= 2.75 and proporcion <= 0.28:
        return elegir(rng, ["bow", "bow", "staff"])

    return elegir(rng, ["sword", "sword", "spear", "mace"])


# --------------------------------------------------------------------------
# Localizacion de capas
# --------------------------------------------------------------------------

def hojas_con(base, anim):
    """Rutas relativas bajo `base` que tienen la animacion pedida."""
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
    """Tamano de la hoja, o None si no se puede abrir."""
    try:
        with Image.open(ruta) as im:
            return im.size
    except Exception:
        return None


# Segmentos de ruta que marcan una capa trasera, no la que se ve delante.
DETRAS = ("universal_behind", "background", "behind", "bg")


def armas_con(categoria, anim):
    """Armas de una categoria con hoja de esa animacion y su capa trasera.

    Cada arma cuelga a una profundidad distinta (unas en walk/, otras bajo
    universal/ o por genero), asi que se busca recursivamente cualquier carpeta
    con el nombre de la animacion. Se descartan las dibujadas en el formato
    grande (1664x512): no casan con las demas capas y partirian al heroe."""
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
    """Busca la capa que va detras del cuerpo para esa misma arma."""
    base = os.path.dirname(os.path.dirname(delante))

    for marca in DETRAS:
        candidato = os.path.join(base, marca, anim, fichero)
        if os.path.isfile(candidato) and mide(candidato) == LIENZO:
            return candidato

        candidato = os.path.join(base, anim, marca, fichero)
        if os.path.isfile(candidato) and mide(candidato) == LIENZO:
            return candidato

    return None


def escudos_con(anim):
    """Escudos con parte delantera y su reverso. Cada modelo cuelga a una profundidad
    distinta (algunos por genero, otros por variante), asi que se busca recursivamente
    cualquier carpeta fg/ que tenga la animacion y se empareja con su bg/ hermana."""
    salida = []
    raiz = os.path.join(HOJAS, "shield")
    if not os.path.isdir(raiz):
        return salida

    objetivo = anim + ".png"
    for carpeta, _, ficheros in os.walk(raiz):
        if os.path.basename(carpeta) != "fg" or objetivo not in ficheros:
            continue

        fg = os.path.join(carpeta, objetivo)
        if mide(fg) != LIENZO:
            continue

        bg = os.path.join(os.path.dirname(carpeta), "bg", objetivo)
        salida.append((fg, bg if os.path.isfile(bg) else None))

    return sorted(salida)


def peinados_con(anim):
    """Peinados con su hoja de delante y, si la tiene, la de detras.

    53 estilos vienen partidos en bg/ y fg/ (melenas que caen por detras y por
    delante del cuerpo). Coger solo el bg dejaba al heroe calvo: su fotograma
    frontal esta vacio. Aqui se emparejan y los bg sueltos se descartan."""
    salida = []
    raiz = os.path.join(HOJAS, "hair")
    if not os.path.isdir(raiz):
        return salida

    objetivo = anim + ".png"
    for carpeta, _, ficheros in os.walk(raiz):
        if objetivo not in ficheros:
            continue

        hoja = os.path.basename(carpeta)
        if hoja == "bg":
            continue

        delante = os.path.join(carpeta, objetivo)
        if mide(delante) != LIENZO:
            continue

        detras = None
        if hoja == "fg":
            candidato = os.path.join(os.path.dirname(carpeta), "bg", objetivo)
            if os.path.isfile(candidato) and mide(candidato) == LIENZO:
                detras = candidato

        salida.append((delante, detras))

    return sorted(salida)


def nombre_peinado(ruta):
    """Primer tramo bajo hair/; es el nombre del estilo."""
    marca = os.sep + "hair" + os.sep
    corte = ruta.find(marca)
    if corte < 0:
        return "?"

    return ruta[corte + len(marca):].split(os.sep)[0]


def cuerpo(tipo, anim):
    return os.path.join(HOJAS, "body", "bodies", tipo, anim + ".png")


# --------------------------------------------------------------------------
# Recoloreado por paleta
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
    """Cambia una rampa por otra, color a color y por posicion en la rampa."""
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


# --------------------------------------------------------------------------
# Composicion
# --------------------------------------------------------------------------

def pegar(lienzo, ruta, rampa=None, familia=None, rampas=None):
    """Superpone una capa sobre el lienzo; devuelve False si el fichero no existe."""
    if not ruta or not os.path.isfile(ruta):
        return False

    capa = Image.open(ruta).convert("RGBA")

    if rampa and rampas:
        base = {"hair": BASE_PELO, "eye": BASE_OJOS}.get(familia, BASE_PIEL)
        capa = recolorear(capa, rampas.get(base), rampas.get(rampa))

    if capa.size != lienzo.size:
        return False

    lienzo.alpha_composite(capa)
    return True


# Nivel de equipo por rareza: cuanto mas raro, mejor pinta el heroe. Para el 1* se usan
# solo las familias que cubren el torso entero; las de tirantes dejaban al heroe en cueros.
TORSO_POR_RAREZA = {
    1: ("torso/clothes/longsleeve", None),
    2: ("torso/armour/leather", None),
    3: ("torso/chainmail", None),
    4: ("torso/armour/legion", "cape/trim"),
    5: ("torso/armour/plate", "cape/solid"),
}

# Piernas y calzado suben de rareza igual que el torso: de pantalon sencillo a placas.
PIERNAS_POR_RAREZA = {
    1: "legs/pantaloons",
    2: "legs/leggings",
    3: "legs/cuffed",
    4: "legs/armour/plate",
    5: "legs/armour/plate",
}
CALZADO_POR_RAREZA = {
    1: "feet/shoes/basic",
    2: "feet/boots/basic",
    3: "feet/boots/fold",
    4: "feet/armour/plate",
    5: "feet/armour/plate",
}

# Hombreras: solo rematan la armadura completa de las rarezas altas.
HOMBRO_POR_RAREZA = {4: "shoulders/epaulets", 5: "shoulders/pauldrons"}

# El pelo de color imposible se reserva a las rarezas altas; los comunes van naturales.
PELO_NATURAL = ("ash", "black", "blonde", "carrot", "chestnut", "dark_brown", "dark_gray",
                "ginger", "gold", "gray", "light_brown", "platinum", "raven", "redhead",
                "sandy", "strawberry", "white")

CATEGORIA_ARMA = {
    "sword": "sword",
    "spear": "polearm",
    "bow": "ranged",
    "staff": "magic",
    "mace": "blunt",
    "shield": "sword",
}

CUERPOS_MASCULINOS = ("male", "muscular", "teen")
CUERPOS_FEMENINOS = ("female",)

# Reparto fijo del catalogo: 30 heroes masculinos / 20 femeninos (60% / 40%).
PROPORCION_MASCULINA = 0.6


def asignar_generos(heroes):
    """Genero por heroe, estable ante relecturas: se ordena por nombre (no por
    orden de disco) y se baraja con semilla fija, asi el reparto 30M/20F cae
    siempre sobre los mismos heroes aunque cambie el listado del directorio."""
    nombres = sorted(hero["heroName"] for hero in heroes)

    rng = random.Random("genero:reparto")
    barajado = list(nombres)
    rng.shuffle(barajado)

    corte = round(len(barajado) * PROPORCION_MASCULINA)
    return {nombre: ("male" if i < corte else "female") for i, nombre in enumerate(barajado)}

# Cabeza humana que encaja con cada tipo de cuerpo; el musculoso usa la masculina.
CABEZA_POR_CUERPO = {
    "male": "male",
    "muscular": "male",
    "female": "female",
    "teen": "male_small",
    "child": "child",
    "pregnant": "female",
}

NARICES = ("button", "big", "straight")

# Tonos de piel creibles. La paleta LPC trae ademas verdes, zombis y pelajes: se dejan
# fuera del reparto normal y solo entran en las rarezas altas, como el pelo de fantasia.
PIEL_NATURAL = ("light", "amber", "olive", "taupe", "bronze", "brown", "black")
PIEL_EXOTICA = ("lavender", "pale_green", "green", "bright_green", "blue")


def nombre_arma(ruta, categoria):
    """Primer tramo bajo la categoria; es el modelo del arma."""
    marca = os.sep + categoria + os.sep
    corte = ruta.find(marca)
    if corte < 0:
        return os.path.splitext(os.path.basename(ruta))[0]

    return ruta[corte + len(marca):].split(os.sep)[0]


def elegir(rng, lista):
    return lista[rng.randrange(len(lista))] if lista else None


def por_tipo(rutas, tipo):
    """Prefiere la variante que encaja con el tipo de cuerpo; si no hay, cualquiera.

    Piernas y calzado no traen corte "female" propio en este set LPC: la silueta
    equivalente es "thin", asi que el cuerpo femenino cae ahi antes de rendirse
    a cualquier variante."""
    encaja = [r for r in rutas if os.sep + tipo + os.sep in r]
    if not encaja and tipo == "female":
        encaja = [r for r in rutas if os.sep + "thin" + os.sep in r]
    return encaja or rutas


def generar(hero, anim, genero, rampas_piel, rampas_pelo, rampas_ojos, cache, verbose=True):
    rng = random.Random(hero["heroName"])
    rareza = max(1, min(5, hero.get("starRank", 1)))
    rol = arquetipo(hero)

    # El genero fija el cuerpo disponible (30M/20F de asignar_generos); dentro de
    # los masculinos, tanques y lanceros salen corpulentos.
    cuerpos_genero = CUERPOS_MASCULINOS if genero == "male" else CUERPOS_FEMENINOS
    if rol in ("shield", "spear") and "muscular" in cuerpos_genero and rng.random() < 0.7:
        tipo = "muscular"
    else:
        tipo = elegir(rng, cuerpos_genero)

    lienzo = Image.new("RGBA", LIENZO, (0, 0, 0, 0))
    capas = []

    torso_base, capa_base = TORSO_POR_RAREZA[rareza]

    # 1. Capa trasera: el reverso de la capa y la parte del arma que va detras.
    if capa_base:
        bg = os.path.join(HOJAS, capa_base, "bg", anim + ".png")
        if pegar(lienzo, bg):
            capas.append("cloak_bg")

    categoria = CATEGORIA_ARMA[rol]
    armas = cache.setdefault(("weapon", categoria), armas_con(categoria, anim))
    arma = elegir(rng, armas)

    if arma and arma[1] and pegar(lienzo, arma[1]):
        capas.append("weapon_behind")

    # El peinado se elige aqui porque su melena trasera va detras del cuerpo.
    pelos = cache.setdefault(("hair", anim), peinados_con(anim))
    pelo = elegir(rng, por_tipo([par[0] for par in pelos], "adult"))
    pelo_detras = None

    for delante, detras in pelos:
        if delante == pelo:
            pelo_detras = detras
            break

    paleta_pelo = sorted(rampas_pelo) if rareza >= 4 else [
        c for c in sorted(rampas_pelo) if c in PELO_NATURAL]
    color_pelo = elegir(rng, paleta_pelo or sorted(rampas_pelo))

    if pegar(lienzo, pelo_detras, color_pelo, "hair", rampas_pelo):
        capas.append("hair_bg")

    # El tanque ademas embraza un escudo: su reverso va detras del cuerpo.
    escudo = None
    if rol == "shield":
        escudos = cache.setdefault(("shield", anim), escudos_con(anim))
        escudo = elegir(rng, escudos)

        if escudo and escudo[1] and pegar(lienzo, escudo[1]):
            capas.append("shield_bg")

    # 2. Cuerpo, con su tono de piel. Los 5* pueden salir con piel exotica.
    tonos = [t for t in PIEL_NATURAL if t in rampas_piel]
    if rareza >= 5:
        tonos += [t for t in PIEL_EXOTICA if t in rampas_piel]

    piel = elegir(rng, tonos or sorted(rampas_piel))
    if pegar(lienzo, cuerpo(tipo, anim), piel, "body", rampas_piel):
        capas.append("body:" + tipo)
    else:
        # Sin ese tipo de cuerpo no hay heroe que valga: se cae a male.
        tipo = "male"
        if not pegar(lienzo, cuerpo(tipo, anim), piel, "body", rampas_piel):
            return None, "sin cuerpo"
        capas.append("body:male")

    # 3a. Piernas y calzado, escalados por rareza (pantalon sencillo -> placas).
    familia_piernas = PIERNAS_POR_RAREZA[rareza]
    piernas = cache.setdefault(("legs", familia_piernas, anim), hojas_con(familia_piernas, anim))
    pieza_piernas = elegir(rng, por_tipo(piernas, tipo))
    if pegar(lienzo, pieza_piernas):
        capas.append("legs:" + familia_piernas.split("/")[-1])

    familia_calzado = CALZADO_POR_RAREZA[rareza]
    calzado = cache.setdefault(("feet", familia_calzado, anim), hojas_con(familia_calzado, anim))
    pieza_calzado = elegir(rng, por_tipo(calzado, tipo))
    if pegar(lienzo, pieza_calzado):
        capas.append("feet:" + familia_calzado.split("/")[-1])

    # 3b. Cabeza: el cuerpo LPC viene sin ella. Va con la misma rampa de piel.
    cabeza = CABEZA_POR_CUERPO.get(tipo, "male")
    ruta_cabeza = os.path.join(HOJAS, "head", "heads", "human", cabeza, anim + ".png")

    if pegar(lienzo, ruta_cabeza, piel, "body", rampas_piel):
        capas.append("head:" + cabeza)
    else:
        return None, "sin cabeza (" + cabeza + ")"

    # 4. Nariz, tambien del tono de la piel, y ojos con su color propio.
    nariz = elegir(rng, list(NARICES))
    ruta_nariz = os.path.join(HOJAS, "head", "nose", nariz, "adult", anim + ".png")
    if pegar(lienzo, ruta_nariz, piel, "body", rampas_piel):
        capas.append("nose:" + nariz)

    color_ojos = elegir(rng, sorted(rampas_ojos))
    ruta_ojos = os.path.join(HOJAS, "eyes", "human", "adult", "default", anim + ".png")
    if pegar(lienzo, ruta_ojos, color_ojos, "eye", rampas_ojos):
        capas.append("eyes:" + color_ojos)

    # 5. Armadura o ropa segun la rareza.
    torsos = cache.setdefault(("torso", torso_base), hojas_con(torso_base, anim))
    torso = elegir(rng, por_tipo(torsos, tipo))
    if pegar(lienzo, torso):
        capas.append("torso")

    # 5b. Hombreras: remate de armadura completa, solo en las dos rarezas mas altas.
    familia_hombro = HOMBRO_POR_RAREZA.get(rareza)
    if familia_hombro:
        hombros = cache.setdefault(("shoulders", familia_hombro, anim), hojas_con(familia_hombro, anim))
        pieza_hombro = elegir(rng, por_tipo(hombros, tipo))
        if pegar(lienzo, pieza_hombro):
            capas.append("shoulders:" + familia_hombro.split("/")[-1])

    # 6. Pelo, sobre la cabeza y ya teñido con el color elegido mas arriba.
    if pegar(lienzo, pelo, color_pelo, "hair", rampas_pelo):
        capas.append("hair:" + nombre_peinado(pelo) + "/" + color_pelo)

    # 7. Delantera de la capa y arma, que van por encima de todo.
    if capa_base:
        fg = os.path.join(HOJAS, capa_base, "fg", anim + ".png")
        if pegar(lienzo, fg):
            capas.append("cloak_fg")

    if arma and pegar(lienzo, arma[0]):
        capas.append("weapon:" + nombre_arma(arma[0], categoria))

    if escudo and pegar(lienzo, escudo[0]):
        capas.append("shield:" + os.path.basename(
            os.path.dirname(os.path.dirname(os.path.dirname(escudo[0])))))

    if verbose:
        print(f"  {hero['heroName']:<10} {rareza}* {rol:<7} {tipo:<9} -> {', '.join(capas)}")

    return lienzo, None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--anim", default="walk", help="animacion LPC a componer (walk por defecto)")
    ap.add_argument("--dry-run", action="store_true", help="no escribe ningun PNG")
    args = ap.parse_args()

    if not os.path.isdir(HOJAS):
        print("No esta tools/LPC_Assets. Clona el repositorio del generador antes.", file=sys.stderr)
        return 1

    heroes = leer_heroes()
    if not heroes:
        print("No hay HeroData en " + HEROES, file=sys.stderr)
        return 1

    rampas_piel = cargar_rampas("body")
    rampas_pelo = cargar_rampas("hair")
    rampas_ojos = cargar_rampas("eye")
    generos = asignar_generos(heroes)

    if not args.dry_run:
        os.makedirs(SALIDA, exist_ok=True)

    n_m = sum(1 for g in generos.values() if g == "male")
    print(f"Generando {len(heroes)} spritesheet(s) con la animacion '{args.anim}' "
          f"({n_m}M / {len(generos) - n_m}F):")

    cache = {}
    hechos = 0
    fallos = []

    for hero in heroes:
        genero = generos[hero["heroName"]]
        lienzo, error = generar(hero, args.anim, genero, rampas_piel, rampas_pelo, rampas_ojos, cache)
        if lienzo is None:
            fallos.append((hero["heroName"], error))
            continue

        if not args.dry_run:
            lienzo.save(os.path.join(SALIDA, hero["asset"] + ".png"))

        hechos += 1

    print(f"\n{hechos} spritesheet(s) generado(s) en {SALIDA}")
    for nombre, error in fallos:
        print(f"  FALLO {nombre}: {error}", file=sys.stderr)

    return 0 if not fallos else 2


if __name__ == "__main__":
    sys.exit(main())
