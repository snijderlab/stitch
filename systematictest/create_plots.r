

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

se <- function(x) sqrt(var(x)/length(x))

results <- read.csv(file="C:/Users/douwe/source/repos/research-project-amino-acid-alignment/systematictest/results.csv", sep="|", row.names=NULL)
results$Type <- as.factor(results$Type)
results$Variant <- as.factor(results$Variant)
results$Proteases <- factor(results$Proteases, label = c("All", "T+C+Alp", "T+C+Aspec", "T+C+LysC"))#, levels = c("All", "T+C+Aspec", "T+C+LysC", "T+C+Alp"))
results$Percentage <- as.factor(results$Percentage)
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
  scale_x_discrete(name ="Proteases used", limits=c("All", "T+C+Aspec", "T+C+LysC", "T+C+Alp")) +
  #geom_sina(aes(color = Percentage), size = .5) +
  scale_color_manual(values =  c("#006278", "#44c4f2", "#3bbcb0", "#e4e41e"))


# Assembler settings - Sinaplot
filtered = results[(results$Percentage == 75 | results$Percentage == 100) & (results$Proteases == "All"),]
#filtered$Percentage = as.factor(filtered$Percentage)
ggplot(filtered, aes(K, Score)) +
  #geom_violin(aes(color = Alphabet), width = .5, size = .75, position = position_dodge(.9)) +
  stat_summary(
    aes(color = Alphabet), fun.data="mean_sdl",  fun.args = list(mult=1), 
    geom = "pointrange",  size = 1, position= position_dodge(.9)
  ) +
  #geom_sina(aes(color = Alphabet), size = .5) +
  scale_color_manual(values =  c("#006278", "#44c4f2", "#3bbcb0", "#e4e41e"))


# OriginalScore versus coverage
filtered = results
filtered$Percentage = as.factor(filtered$Percentage)
ggplot(filtered, aes(OriginalScore, Total)) +
  geom_point(aes(color = Percentage)) +
  #theme(axis.text.x = element_text(angle = 90, hjust = 1)) +
  scale_x_log10() +
  scale_color_manual(values =  c("#006278", "#44c4f2", "#3bbcb0", "#e4e41e"))


# OriginalScore versus score
filtered = results
filtered$Percentage = as.factor(filtered$Percentage)
ggplot(filtered, aes(OriginalScore, Score)) +
  geom_point(aes(color = Percentage)) +
  #theme(axis.text.x = element_text(angle = 90, hjust = 1)) +
  #scale_x_continuous(limits=c(0, 10000)) +
  scale_x_log10() +
  scale_color_manual(values =  c("#006278", "#44c4f2", "#3bbcb0", "#e4e41e"))


# Fancy coverageplot
filtered = results[(results$Percentage == 75 | results$Percentage == 100) & (results$Proteases == "All"),]
length = c(25,8,17,8,38,12,11,330,25,7,17,3,36,10,10,106)
values = c(mean(filtered$HcF1, na.rm=TRUE),mean(filtered$HcCDR1, na.rm=TRUE),mean(filtered$HcF2, na.rm=TRUE),mean(filtered$HcCDR2, na.rm=TRUE),mean(filtered$HcF3, na.rm=TRUE),mean(filtered$HcCDR3, na.rm=TRUE),mean(filtered$HcF4, na.rm=TRUE),mean(filtered$HcC, na.rm=TRUE), mean(filtered$LcF1, na.rm=TRUE),mean(filtered$LcCDR1, na.rm=TRUE),mean(filtered$LcF2, na.rm=TRUE),mean(filtered$LcCDR2, na.rm=TRUE),mean(filtered$LcF3, na.rm=TRUE),mean(filtered$LcCDR3, na.rm=TRUE),mean(filtered$LcF4, na.rm=TRUE),mean(filtered$LcC, na.rm=TRUE))
labels = 1:16
chain = c("H", "H", "H", "H", "H", "H", "H", "H", "L", "L", "L", "L", "L", "L", "L", "L")
errors = c(se(filtered$HcF1),se(filtered$HcCDR1),se(filtered$HcF2),se(filtered$HcCDR2),se(filtered$HcF3),se(filtered$HcCDR3),se(filtered$HcF4),se(filtered$HcC), se(filtered$LcF1),se(filtered$LcCDR1),se(filtered$LcF2),se(filtered$LcCDR2),se(filtered$LcF3),se(filtered$LcCDR3),se(filtered$LcF4),se(filtered$LcC))

coverage = data.frame(length, values, labels, chain, errors)
coverage$chain = factor(coverage$chain, label=c("Heavy Chain", "Light Chain"))
coverage$labels = as.factor(coverage$labels)

coverage$right <- cumsum(coverage$length) + 2*c(0:(nrow(coverage)-1))
coverage$left <- coverage$right - coverage$length

ggplot(coverage, aes(ymin=.9)) + 
  #geom_bar(stat="identity") +
  geom_rect(aes(xmin = left, xmax = right, ymax = values, fill = chain)) +
  geom_rect(aes(xmin = left+(right-left)/2-1, xmax = left+(right-left)/2+1, ymin=values-errors, ymax = values+errors, fill = "Standard Error")) +
  #geom_errorbar(aes(ymin=values-errors, ymax=values+errors), width=.2, position=position_dodge(.9)) +
  scale_x_discrete(labels=c("1"="F1", "2"="CDR1", "3"="F2", "4"="CDR2", "5"="F3", "6"="CDR3", "7"="F4", "8"="Constant", "9"="F1", "10"="CDR1", "11"="F2", "12"="CDR2", "13"="F3", "14"="CDR3", "15"="F4", "16"="Constant")) +
  scale_fill_manual(values =  c("#3bbcb0", "#44c4f2", "#006278", "#e4e41e"))

