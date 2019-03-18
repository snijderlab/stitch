#!/usr/bin/pip
from pyteomics.parser import cleave, expasy_rules
import sys

number = int(sys.argv[1])
sequence = sys.argv[2]

cleave(sequence, expasy_rules['trypsin'], expasy_rules['pepsin'])