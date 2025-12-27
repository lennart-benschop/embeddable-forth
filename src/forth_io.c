/* Embeddable C-based FORTH interpreter.
   Copyright 2025 L.C. Benschop, Vught, The Netherlands.
   The program is released under the MIT license.
   There is NO WARRANTY.
*/

/* POSIX version */

#include <stdio.h>
#include <string.h>
#include <signal.h>
#include <unistd.h>
#include <errno.h>
#include <termios.h>
#include <fcntl.h>
#include <sys/time.h>
#include <stdint.h>
#include "forth_engine.h"
#include <stdlib.h>

static struct termios oldterm;
static struct termios newterm;
static int tflags;
static int realterm;
int keybuf,raw;
static int childpid;

#ifndef O_NDELAY
#define O_NDELAY O_NONBLOCK
#endif

void inthandler(int s)
{
 signal(SIGINT,inthandler);
 set_irq(ENGINE_BREAK_IRQ);
}

void divhandler(int s)
{
  printf("Divide overflow\n");
  signal(SIGFPE,divhandler);
  set_irq(ENGINE_DIVIDE_IRQ);
}


static void forthterm(void);
static void systerm(void);


static void quithandler(int s)
{
 systerm();
 printf("Quit!\n");exit(1);
}

#ifdef SIGTSTP
static void stophandler(int s)
{
 if(raw){systerm();raw=1;}
 //raise(SIGTSTP);
 raise(SIGSTOP);
}

static void conthandler(int s)
{
 signal(SIGTSTP,stophandler);
 signal(SIGCONT,conthandler);
 if(raw)forthterm(); 
}
#endif

static void alarmhandler(int s)
{
  set_irq(ENGINE_ALARM_IRQ);
}

static int f_argc;
static char **f_argv;
void forth_io_init(int argc, char **argv)
{
  f_argc=argc;
  f_argv=argv;
  realterm=isatty(0);
  if(realterm) {
    tcgetattr(0,&oldterm);
    newterm=oldterm;
    newterm.c_iflag = newterm.c_iflag & ~INLCR & ~ICRNL;
    newterm.c_lflag = newterm.c_lflag & ~ECHO & ~ICANON;
    newterm.c_cc[VMIN]=1;  
    newterm.c_cc[VTIME]=0;
    tflags=fcntl(0,F_GETFL,0);
  } 
  signal(SIGQUIT,quithandler);
  signal(SIGINT,inthandler);
  //signal(SIGFPE,divhandler);
#ifdef SIGTSTP
  signal(SIGTSTP,stophandler);
  signal(SIGCONT,conthandler);
#endif 
}

void forth_io_exit(void)
{
  systerm();
}

static void forthterm(void)
{
 if(realterm) {
  tcsetattr(0,TCSAFLUSH,&newterm);
  fcntl(0,F_SETFL,tflags|O_NDELAY);
  keybuf=EOF;
  raw=1;
 } 
}

static void systerm(void) 
{
 if(realterm) {
  tcsetattr(0,TCSAFLUSH,&oldterm);
  fcntl(0,F_SETFL,tflags);
  raw=0;
 } 
}

static int getch(void)
{
  int c;
  if(realterm && raw) {
    if(keybuf==EOF) {
      fcntl(0,F_SETFL,tflags);
      while((keybuf=getchar())==EOF&&errno==EINTR)
	;   
      fcntl(0,F_SETFL,tflags|O_NDELAY);
    } 
    c=keybuf;keybuf=EOF;
    return c; 
  } else return getchar();
}

static int kbhit(void)
{
  if(realterm && raw) {
    if(keybuf!=EOF) return 1;
    keybuf=getchar();
    return (keybuf != EOF );
  } else return 1;
}

static void putch(int c)
{
  int res;
  char k=c;
  if(raw)while(write(1,&k,1)<0);   
  else putchar(c);
}

struct itimerval tt;

static void setalarm(unsigned int usecs)
{
 signal(SIGALRM,alarmhandler);
 tt.it_interval.tv_sec=0;
 tt.it_interval.tv_usec=0;
 tt.it_value.tv_sec=usecs/1000000;
 tt.it_value.tv_usec=usecs%1000000; 
 setitimer(ITIMER_REAL,&tt,0);
}

#define MAKE_ASCIIZ(start,len) (name_addr=start,name_len=len,	\
				   savechr=*(start+len),*(start+len)=0)
FILE *fileids[20];
char *filemodes[]={"r","rb","w","wb","r+","r+b"};

void forth_io(uint8_t opcode, struct engine_state *state)
{
  uint32_t *sp = state->sp;
  uint8_t *dict_base = state->dict;
  uint8_t savechr;
  uint8_t*name_addr=0;
  uint32_t name_len;
  int rc;
  switch (opcode) {
  case 0: /* KEY */
    *--sp=getch();
    break;
  case 1:
    *--sp=kbhit();
    break;
  case 2: /* ACCEPT */
    {
      char *p = dict_base + sp[1];
      uint8_t c;
      uint32_t i=0;
      do {
	c = getch();
	if (c == '\b' || c==0x7f) {
	  if (i>0) {
	    putch('\b');putch(' ');putch('\b'); i--;p--;
	  }
	} else if (i < sp[0] && c>=' ' && c<='~') {
	  *p++=c;putch(c);i++;
	}	 
      } while (c != '\r' && c != '\n');
      putch(' ');
      sp++;
      sp[0] = i;
    }
    break;
  case 3: /* EMIT */
    putch(*sp++);
    break;
  case 4: /* TYPE */
    { 
      uint32_t i;
      char *p=dict_base + sp[1];
      for (i=0; i<sp[0]; i++)
	putch(*p++);
      sp+=2;
    }
    break;
  case 5: /* BYE */
    putch('\n');
    set_irq(ENGINE_EXIT_IRQ);
    break;
  case 9: /* SETTERM */		
    if (sp++)
      forthterm();
    else
      systerm();
    break;
  case 0xa: /* SETALARM */
    setalarm(*sp++);
    break;
  case 0x10: /* OPEN-FILE */
    MAKE_ASCIIZ(dict_base+sp[2],sp[1]);
    if (sp[0]>=6) {
      sp++;
      sp[1] = 0;
      sp[0] = -202;
    } else {
      int i;
      for(i=0;i<20;i++) {
	if(!fileids[i])break;
      } 
      if(i==20) {
	sp++;
	sp[1]=0;
	sp[0]=-201;
      } else {
	fileids[i] = fopen(name_addr,filemodes[sp[0]]);
	if (fileids[i] == 0) {
	  sp++;
	  sp[1]=0;
	  sp[0]=-200;
	} else {
	  sp++;
	  sp[1]=i+1;
	  sp[0]=0;
	}
      }
    }
    break;
  case 0x11: /* CLOSE-FILE */
    sp[0]--;
    if(sp[0]>=20 || fileids[sp[0]]==0) {
      sp[0]=-201;
    } else {
      fclose(fileids[sp[0]]);
      fileids[sp[0]] = 0;
      sp[0] = 0;      
    }
    break;
  case 0x12: /* READ-FILE */
    sp[0]--;
    if(sp[0]>=20 || fileids[sp[0]]==0) {
      sp[0]=-201;
    } else {
      clearerr(fileids[sp[0]]);
      rc=fread(dict_base+sp[2],1,sp[1],fileids[sp[0]]);
      if (ferror(fileids[sp[0]])) {
	sp+=1;
	sp[1] = 0;
	sp[0] = -200;
      } else {
	sp[1] = rc;
	sp[0] = 0;
      }
    }
    break;
  case 0x13: /* WRITE-FILE */
    sp[0]--;
    if(sp[0]>=20 || fileids[sp[0]]==0) {
      sp+=2;
      sp[0]=-201;
    } else {
      clearerr(fileids[sp[0]]);
      fwrite(dict_base+sp[2],1,sp[1],fileids[sp[0]]);
      if (ferror(fileids[sp[0]])) {
	sp+=2;
	sp[0] = -200;
      } else {
	sp+=2;
	sp[0] = 0;
      }
    }
    break;
  case 0x14: /* REPOSITION-FILE */
    sp[0]--;
    if(sp[0]>=20 || fileids[sp[0]]==0) {
      sp+=2;
      sp[0]=-201;
    } else {
      long pos = ((long)sp[1]) << 32 | sp[2];
      rc = fseek(fileids[sp[0]], pos, SEEK_SET);
	sp+=2;
      sp[0] = rc;
    }
    break;
  case 0x15: /* FILE-POSITION */
    sp[0]--;
    if(sp[0]>=20 || fileids[sp[0]]==0) {
      sp-=2;
      sp[2]=0;
      sp[1]=0;
      sp[0]=-201;
    } else {
      long pos = ftell(fileids[sp[0]]);
      sp-=2;
      sp[2] = pos;
      sp[1] = pos >> 32;
      sp[0] = 0;
    }
    break;
  case 0x16: /* READ-LINE */
    sp[0]--;
    if(sp[0]>=20 || fileids[sp[0]]==0) {
      sp[0]=-201;
    } else {
      char *p;
      clearerr(fileids[sp[0]]);
      p = fgets(dict_base+sp[2],sp[1],fileids[sp[0]]);
      if (ferror(fileids[sp[0]])) {
	sp[2] = 0;
	sp[1] = 0;
	sp[0] = -200;
      } else {
	uint32_t l=strlen(dict_base+sp[2]);
	if (l==0 || feof(fileids[sp[0]]) || p==NULL) {
	  sp[2] = 0;
	  sp[1] = 0;
	  sp[0] = 0;
	} else {
	  if (*(dict_base+sp[2]+l-1)=='\n') l--;
	  sp[2] = l;
	  sp[1] = -1;
	  sp[0] = 0;
	}
      }      
    }
    break;
  case 0x17: /* WRITE-LINE */
    sp[0]--;
    if(sp[0]>=20 || fileids[sp[0]]==0) {
      sp[0]=-201;
    } else {
      clearerr(fileids[sp[0]]);
      fwrite(dict_base+sp[2],1,sp[1],fileids[sp[0]]);
      fputc('\n',fileids[sp[0]]);
      if (ferror(fileids[sp[0]])) {
	sp+=2;
	sp[0] = -200;
      } else {
	sp+=2;
	sp[0] = 0;
      }
    }
    break;
  case 0x18: /* DELETE-FILE */
    MAKE_ASCIIZ(dict_base+sp[1],sp[0]);
    rc=remove(name_addr);
    sp++;
    sp[0] = rc;
    break;
  case 0x19: /* SYSTEM */
    MAKE_ASCIIZ(dict_base+sp[1],sp[0]);
    systerm();
    rc=system(name_addr);
    forthterm();
    sp++;
    sp[0] = rc;
    break;
  case 0x1A: /* FILE-SIZE */
    sp[0]--;
    if(sp[0]>=20 || fileids[sp[0]]==0) {
      sp-=2;
      sp[2] = 0;
      sp[1] = 0;
      sp[0]=-201;
    } else {
      long oldpos = ftell(fileids[sp[0]]);
      long pos;
      fseek(fileids[sp[0]],0,SEEK_END);
      pos = ftell(fileids[sp[0]]);
      fseek(fileids[sp[0]],oldpos,SEEK_SET);
      sp-=2;
      sp[2] = pos;
      sp[1] = pos >> 32;
      sp[0] = 0;
    }
    break;
  case 0x1D: /* CHDIR */
    MAKE_ASCIIZ(dict_base+sp[1],sp[0]);
    rc = chdir(name_addr);
    sp++;
    sp[0] = rc;
    break;
  case 0x1E: /* ARG@ */
    if (sp[0]+1 < f_argc) {
      int len = strlen(f_argv[sp[0]+1]);
      if (len>sp[1]) len = sp[1];
      memcpy(dict_base+sp[2], f_argv[sp[0]+1], len);
      sp++;
      sp[0] = len;
    } else {
      sp++;
      sp[0] =0;
    }
    break;
  default:
    /* unknown opcode */
    printf("Illegal OS call %08x\n",opcode);
  }
  /* Restore any name that got a terminating NUL */
  if (name_addr) {
    *(name_addr + name_len) = savechr;
  }
  state->sp = sp;
}
