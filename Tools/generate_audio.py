"""Original sample synthesis for Boba Brawlers. No third-party recordings."""
from pathlib import Path
import math, wave
import numpy as np

OUT = Path(__file__).resolve().parents[1] / 'Assets/BobaBrawlers/Resources/Audio'
OUT.mkdir(parents=True, exist_ok=True)
RATE = 22050
rng = np.random.default_rng(42)
def save(name, data):
    data = np.asarray(data, dtype=np.float64)
    data = np.tanh(data * .85)
    with wave.open(str(OUT / (name + '.wav')), 'wb') as f:
        f.setnchannels(1 if data.ndim == 1 else 2); f.setsampwidth(2); f.setframerate(RATE)
        f.writeframes((np.clip(data, -1, 1) * 30000).astype('<i2').tobytes())
def note(midi, duration, volume=.2, instrument='bell'):
    t = np.arange(int(duration * RATE)) / RATE
    hz = 440 * 2 ** ((midi - 69) / 12)
    if instrument == 'bass':
        sound = np.sin(2*np.pi*hz*t) + .2*np.sin(4*np.pi*hz*t)
        env = np.minimum(t/.012, 1) * np.exp(-t*4)
    else:
        sound = np.sin(2*np.pi*hz*t) + .28*np.sin(4*np.pi*hz*t) + .12*np.sin(6*np.pi*hz*t)
        env = np.minimum(t/.006, 1) * np.exp(-t*(6 if instrument=='bell' else 3))
    return sound * env * volume
def music(name, bpm, menu=False):
    beat = 60 / bpm; beats = 32; length = int(beats * beat * RATE)
    data = np.zeros((length, 2))
    chords = [(60,64,67,71),(57,60,64,67),(53,57,60,64),(55,59,62,65)]
    melody = [76,79,83,79,76,74,72,74,76,79,81,79,76,74,72,71]
    def add(signal, start, pan=0):
        offset = int(start * RATE); n = min(len(signal), length-offset)
        if n>0: data[offset:offset+n,0] += signal[:n]*(.72-pan*.25); data[offset:offset+n,1] += signal[:n]*(.72+pan*.25)
    for b in range(beats):
        chord = chords[(b//8)%4]
        add(note(chord[0]-24, beat*.9, .23, 'bass'), b*beat)
        for k in range(2):
            pitch = chord[(b*2+k)%4]+12
            add(note(pitch, beat*.7, .055 if menu else .075), (b+k*.5)*beat, (-1 if k==0 else 1)*.7)
        if b%2==0:
            add(note(melody[(b//2)%len(melody)], beat*1.6, .13 if menu else .17), b*beat, .25)
        t = np.arange(int(.18*RATE))/RATE
        kick = np.sin(2*np.pi*(60*t+40*.035*(1-np.exp(-t/.035)))) * np.exp(-t*25) * (.12 if menu else .24)
        if b%2==0 or not menu: add(kick,b*beat)
        for k in range(2):
            t=np.arange(int(.06*RATE))/RATE
            hat=rng.normal(0,1,len(t))*np.exp(-t*100)*(.018 if menu else .032)
            add(hat,(b+k*.5)*beat,(-1 if k==0 else 1)*.5)
        if b%4==2:
            t=np.arange(int(.13*RATE))/RATE
            snare=(rng.normal(0,1,len(t))*.16+np.sin(2*np.pi*180*t)*.13)*np.exp(-t*25)
            add(snare,b*beat)
    # Circular delay preserves loop continuity rather than adding an unmatched tail.
    data += np.roll(data, int(beat*.75*RATE), axis=0)*.16
    save(name,data)
music('MenuLoop',112,True); music('BrawlLoop',138)
for name, start, end, duration in [('Dash',750,140,.19),('Impact',240,65,.14),('KO',540,160,.32),('Button',800,1200,.07)]:
    t=np.arange(int(duration*RATE))/RATE
    phase=2*np.pi*(start*t+(end-start)*t*t/(2*duration))
    sound=np.sin(phase)*np.sin(np.pi*t/duration)*.35
    if name=='Impact': sound+=rng.normal(0,.12,len(t))*np.exp(-t*35)
    save(name,sound)
t=np.arange(RATE//2)/RATE
save('Charge', .11*np.sin(2*np.pi*220*t)+.035*np.sin(2*np.pi*440*t))
for name, notes in [('Victory',[72,76,79,84,83,84]),('Defeat',[72,68,65,60])]:
    data=np.zeros(int(2*RATE))
    for i,pitch in enumerate(notes):
        signal=note(pitch,.4,.32); start=int(i*.22*RATE); data[start:start+len(signal)]+=signal
    save(name,data)
# A clearly original, synthetic cartoon vowel; no human/meme recording is used.
t=np.arange(int(.7*RATE))/RATE; f0=240-135*(t/.7)**.65
phase=np.cumsum(2*np.pi*f0/RATE); vowel=np.zeros(len(t))
for harmonic in range(1,19):
    hz=f0*harmonic
    weight=sum(np.exp(-.5*((hz-formant)/width)**2)*gain for formant,width,gain in [(800,170,1),(1200,230,.6),(2500,350,.25)])
    vowel+=np.sin(phase*harmonic)*weight/harmonic
envelope=np.minimum(t/.07,1)*np.minimum((.7-t)/.16,1)
save('ComicFall',(vowel*1.2+rng.normal(0,.012,len(t)))*envelope)
print('Generated',len(list(OUT.glob('*.wav'))),'original audio files at',OUT)
