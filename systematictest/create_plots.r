

#data = data[2:1052, 1:8]

#plot(data)
#boxplot(data[,7]~data[,6], main="K versus score", ylab="Score", xlab="K")
#boxplot(data[,7]~data[,5], main="Percentage versus score", ylab="Score", xlab="Percentage")
#boxplot(data[,7]~data[,4], main="Proteases versus score", ylab="Score", xlab="Proteases")
#boxplot(data[,7]~data[,2], main="Variant versus score", ylab="Score", xlab="Variant")
#boxplot(data[,7]~data[,1], main="Type versus score", ylab="Score", xlab="Type")

library(dplyr) 
library(ggplot2)
#library(ggpubr)
library(ggforce)
library(Hmisc)

theme_set(
  theme_bw()
)

results <- read.csv(file="C:/Users/douwe/source/repos/research-project-amino-acid-alignment/systematictest/results.csv", sep="|", row.names=NULL)
results$Type <- as.factor(results$Type)
results$Variant <- as.factor(results$Variant)
results$Proteases <- factor(results$Proteases, label = c("All", "T+C+Alp", "T+C+Aspec", "T+C+LysC"))#, levels = c("All", "T+C+Aspec", "T+C+LysC", "T+C+Alp"))
#results$Percentage <- as.factor(results$Percentage)
results$Alphabet <- factor(results$Alphabet, label = c("Identity", "Common Errors"))
results$K <- as.factor(results$K)

filtered = results
filtered$Percentage = as.factor(filtered$Percentage)
# Input importance - Sinaplot
ggplot(filtered, aes(Proteases, Score)) +
  #geom_boxplot(aes(color = Percentage), width = .5, size = .75, position = position_dodge(.9)) +
  stat_summary(
    aes(color = Percentage), fun.data="mean_sdl",  fun.args = list(mult=1), 
    geom = "pointrange",  size = 1, position= position_dodge(.75)#, color = "#000000"
  ) +
  #geom_sina(aes(color = Percentage), size = .5) +
  scale_color_manual(values =  c("#006278", "#44c4f2", "#3bbcb0", "#e4e41e"))

# Assembler settings - Sinaplot
filtered = results[results$Percentage == 100,]
filtered$Percentage = as.factor(filtered$Percentage)
ggplot(filtered, aes(K, Score)) +
  #geom_violin(aes(color = Alphabet), width = .5, size = .75, position = position_dodge(.9)) +
  stat_summary(
    aes(color = Alphabet), fun.data="mean_sdl",  fun.args = list(mult=1), 
    geom = "pointrange",  size = 1, position= position_dodge(.9)
  ) +
  #geom_sina(aes(color = Alphabet), size = .5) +
  scale_color_manual(values =  c("#006278", "#44c4f2", "#3bbcb0", "#e4e41e"))

# Score versus coverage
ggplot(results, aes(Total, Score)) +
  geom_point()

# Fancy coverageplot
lengths = c(25,8,17,8,38,12,11,330,25,7,17,3,36,10,10,106)
ggplot(results, aes())

