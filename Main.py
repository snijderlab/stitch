#import array as arr

input_file = "examples/001/reads.txt"
chunk_length = 5

## Graph datastructure?



## Getting input
# For now just use a minimal implementation, reads separated b whitespace

f = open(input_file, "r")
input_content = f.read()
f.close()

lines = input_content.split('\n')
reads = []

for line in lines:
    if line[0] != '#':
        reads.extend(line.split())

print("All reads " + str(len(reads)))
print(reads)

## Generate all chunks
# All chunks of length (chunk_length)

chunks = []

for read in reads:
    read_len = len(read)
    if read_len > chunk_length:
        for i in range(0, read_len-chunk_length):
            chunks.append(read[i:i+chunk_length])
    elif read_len == chunk_length:
        chunks.append(read)
    # Else: ignore for now

# Invert all chunks because it is not known which way is correct

all_chunks = []

for chunk in chunks:
    all_chunks.append(chunk)
    all_chunks.append(chunk[::-1])

print("All chunks " + str(len(all_chunks)))
print(all_chunks)

## Building the graph
# Generating all overlaps

overlaps = set() # A set because every overlap should only be generated once

for chunk in all_chunks:
    overlaps.add(chunk[0:-1])
    overlaps.add(chunk[1:])

print("All overlaps " + str(len(overlaps)))
print(overlaps)

# Create a node for every possible overlap (one amino acid shifted)

graph = []

for overlap in overlaps:
    graph.append((overlap, []))

# Connect the nodes based on the chunks

for chunk in all_chunks:
    for node in graph:
        if node[0] == chunk[0:-1]:
            for index, node2 in enumerate(graph):
                if node2[0] == chunk[1:]:
                    node[1].append(index)

print("Graph")
print(graph)

## Finding paths
# Naïve algorithm

current_node = graph[0]

sequence = current_node[0][0:-1]

while True:
    sequence += current_node[0][-1]
    if len(current_node[1]) > 0:
        current_node = graph[current_node[1][0]]
    else:
        break

## Returning output

print("Sequence")
print(sequence)