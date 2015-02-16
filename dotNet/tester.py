import subprocess

quoraFile = open('quora.txt', 'r')
proc = subprocess.Popen(['python', 'typeahead.py'], stdout=subprocess.PIPE)

for line in quoraFile:
