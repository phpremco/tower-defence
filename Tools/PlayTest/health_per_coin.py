#!/usr/bin/env python3
"""Health per coin of every campaign cell, read from the assets (round 75; a reading aid, not a gate).

    python Tools/PlayTest/health_per_coin.py [waves]      # run from the project root; with a number: only the first N waves

Per level and difficulty: the health of every creature but the bosses (count x the difficulty's count x health x the wave's
multiplier x the difficulty's health), the kill coins they pay (reward x the difficulty's income) and the starting money, and
health / (coins + start). It ignores what a creature is (armour, flight) and how fast it comes, so it orders the cells of
one level by how tight their economy is and nothing more.
"""
import glob
import os
import re
import sys

root='Assets/_Project/Data'
names={};hp={};rw={};boss={};fl={}
for f in glob.glob(root+'/Enemies/*.asset'):
    n=os.path.basename(f)[:-6]; t=open(f).read()
    g=re.search(r'guid: (\w+)',open(f+'.meta').read()).group(1); names[g]=n
    hp[n]=float(re.search(r'_maxHealth: ([\d.]+)',t).group(1)); rw[n]=float(re.search(r'_reward: ([\d.]+)',t).group(1))
    boss[n]=re.search(r'_isBoss: 1',t) is not None; fl[n]=re.search(r'_flies: 1',t) is not None
diff={}
for f in glob.glob(root+'/Difficulty/*.asset'):
    t=open(f).read(); g=re.search(r'guid: (\w+)',open(f+'.meta').read()).group(1)
    diff[g]=(os.path.basename(f)[:-6],float(re.search(r'_healthMultiplier: ([\d.]+)',t).group(1)),float(re.search(r'_countMultiplier: ([\d.]+)',t).group(1)),float(re.search(r'_incomeMultiplier: ([\d.]+)',t).group(1)),float(re.search(r'_startingMoneyMultiplier: ([\d.]+)',t).group(1)))
upto=int(sys.argv[1]) if len(sys.argv)>1 else 99
for lv in ['C1_01','C1_02','C1_03','C1_04','C1_05','C1_06']:
    money=float(re.search(r'_startingMoney: (\d+)',open(f'{root}/Levels/Level_{lv}.asset').read()).group(1))
    t=open(f'{root}/Waves/Waves_{lv}.asset').read()
    row=[]
    for part in t.split('  - Difficulty:')[1:]:
        g=re.search(r'guid: (\w+)',part).group(1); name,dh,dc,inc,sm=diff[g]
        H=C=0; 
        for wi,w in enumerate(part.split('    - _startDelay:')[1:],1):
            if wi>upto: break
            mult=float(re.search(r'_healthMultiplier: ([\d.]+)',w).group(1))
            for e,c in re.findall(r'- Enemy: .*?guid: (\w+).*?\n        Count: (\d+)',w):
                n=names[e]
                if boss[n]: continue
                k=max(1,round(int(c)*dc))
                H+=k*hp[n]*mult*dh; C+=k*rw[n]*inc
        row.append(f"{name[:3]} hp {H:7.0f} coins {C:5.0f} start {money*sm:4.0f} hp/coin {H/(C+money*sm):5.2f}")
    print(lv,' | '.join(row))
