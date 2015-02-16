import sys
from sys import stdin

class Item:
	def __init__(self, type, id, score, words, internal_id):
		self.type = type
		self.id = id
		self.score = score
		self.words = words
		self.internal_id = internal_id

	def __str__(self):
		return "%s.%s" % (self.id , self.internal_id)

class Node:
	def __init__(self):
		self.childrens = {}
		self.items = set()

	def add(self, item):
		for word in item.words:
			self.internal_add(item, word)

	def internal_add(self, item, word):
		currentNodeList = quickTree.childrens
		iterator = None

		for letter in word:
			if not letter in currentNodeList:
				iterator = Node()
			else:
				iterator = currentNodeList[letter]

			iterator.items.add(item)
			currentNodeList[letter] = iterator



class Solver:
	command_iter = 1
	
	def add(self, type, id, score, words):
		item = Item(type, id, score, words, Solver.command_iter)
		Solver.command_iter += 1
		quickTree.add(item)

sys.stdin = open('quora.txt', 'r')

quickTree = Node() #Store the nodes for all words
items = {} #Store all items

solver = Solver()

for line in iter(sys.stdin.readline, ''):
	line = line.strip().split()
	
	if line[0] == 'ADD':
		solver.add(line[1], line[2], float(line[3]), [word.lower() for word in line[4:]])

print([a in quickTree.childrens['a'].items])