data <- read.csv(file="C:/Users/douwe/source/repos/research-project-amino-acid-alignment/systematictest/results.csv", header=FALSE, sep=";")

data = data[2:1052, 1:8]

#plot(data)
boxplot(data[,7]~data[,6], main="K versus score", ylab="Score", xlab="K")
boxplot(data[,7]~data[,5], main="Percentage versus score", ylab="Score", xlab="Percentage")
boxplot(data[,7]~data[,4], main="Proteases versus score", ylab="Score", xlab="Proteases")
boxplot(data[,7]~data[,2], main="Variant versus score", ylab="Score", xlab="Variant")
boxplot(data[,7]~data[,1], main="Type versus score", ylab="Score", xlab="Type")