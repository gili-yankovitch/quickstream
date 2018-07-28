include proj.mk

JFLAGS	:= -g:none

JAVAC	:= java
OBJS	:= $(SRCS:.java=.class)

all: QuickStream

QuickStream: ${OBJS}
	@echo "Packing \e[32m$@\e[0m"
	@jar cfm $@.jar Manifest.txt $(OBJS)

%.class: %.java
	@echo "Building \e[91m$<\e[0m"
	@javac ${JFLAGS} $<

clean:
	@rm -f ${OBJS}
	@rm -f QuickStream.jar
	@echo "\e[34mClean\e[0m"
