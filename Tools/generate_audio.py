"""Generate original synth effects and a seamless ambience loop; no external assets."""
from pathlib import Path
import math
import random
import struct
import wave

ROOT = Path(__file__).resolve().parents[1] / "Assets" / "Audio"
ROOT.mkdir(parents=True, exist_ok=True)
RATE = 22050


def write(name, duration, sample):
    with wave.open(str(ROOT / (name + ".wav")), "wb") as audio:
        audio.setnchannels(1)
        audio.setsampwidth(2)
        audio.setframerate(RATE)
        audio.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, sample(i / RATE, duration))) * 32767))
                                 for i in range(round(duration * RATE))))


def sine(frequency, time):
    return math.sin(2 * math.pi * frequency * time)


def note_sequence(notes, step, volume=.45):
    def sample(t, _):
        index = min(int(t / step), len(notes) - 1)
        age = t - index * step
        envelope = min(age / .008, 1) * math.exp(-age * 12)
        return volume * envelope * (sine(notes[index], age) + .2 * sine(notes[index] * 2, age))
    return sample


write("Collect", .3, note_sequence([660, 990], .15))
write("Checkpoint", .6, note_sequence([330, 440, 660], .2))
write("Win", 1.4, note_sequence([330, 440, 550, 660, 880, 990, 1320], .2))
write("Fall", .5, lambda t, d: .3 * sine(300 * t - 180 * t * t, 1) * (1 - t / d) ** 2)
rng = random.Random(18)
write("Boost", .45, lambda t, d: (rng.uniform(-1, 1) * .22 + sine(180 + 350 * t, t) * .2)
      * math.sin(math.pi * t / d) ** 2)
# Integer cycle counts make the eight-second loop continuous at its seam.
write("Ambience", 8, lambda t, d: .17 * (sine(55, t) + .45 * sine(82.5, t) + .25 * sine(110, t))
      * (.8 + .2 * math.cos(2 * math.pi * t / d)))
print("Generated 6 original WAV assets in", ROOT)
