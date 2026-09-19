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
#include <stdbool.h>
#include <ctype.h>

#ifdef USE_TERM_BITMAP
#include "term_bitmap.h"
#endif

#define HIST_ENTRIES 16
#define HIST_LINELEN 128

static struct termios oldterm;
static struct termios newterm;
static int tflags;
static int realterm;
int keybuf,raw;

#ifdef USE_TERM_BITMAP
static void *tbm;
static unsigned int tbm_width,tbm_height,tbm_ncolors,tbm_mode;
#endif

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

static int ekey(void)
{
  int ch2;
  int ch = getch();
  if (ch == 27 && kbhit()) {
    ch2 = getch();
    if (ch2 == '[' && kbhit()) {
      ch = -getch(); // CHeck the ANSI sequences for cursor keys ESC [ letter
                     // and return them as negative numbers
    }
  }
  return ch;
}

static void putch(int c)
{
  char k=c;
  if(raw)while(write(1,&k,1)<0);   
  else putchar(c);
}

static void typestr(char *p, int len)
{
  int i;
  for(i=0; i<len;i++) putch(p[i]);
}

static void moveleft(int n)
{
  char buf[16];
  if (n>0) {
    sprintf(buf,"\033[%dD",n);
    typestr(buf,strlen(buf));
  }
}

static void moveright(int n)
{
  char buf[16];
  if (n>0) {
    sprintf(buf,"\033[%dC",n);
    typestr(buf,strlen(buf));
  }
}

struct {
  unsigned int len;
  char line[HIST_LINELEN];
} history_buf[HIST_ENTRIES];
unsigned int history_index = 0;

void put_hist(char *p, int len)
{
  if (len > HIST_LINELEN) len = HIST_LINELEN;
  if (len > 0) {
    memcpy(history_buf[history_index].line, p, len);
    history_buf[history_index].len = len;
    history_index++;
    if (history_index == HIST_ENTRIES) history_index = 0;      
  }
}


int get_hist(char *p, int maxlen, int index)
{
  int len = maxlen;
  int idx;
  if (index > 0 && index <= HIST_LINELEN) {
    idx = history_index - index;
    if (idx < 0) idx+=HIST_ENTRIES;
    if (len > history_buf[idx].len) len = history_buf[idx].len;
    memcpy(p, history_buf[idx].line, len);
  }
  return len;
}

static int editline(char *p, int maxlen)
{
  int c;
  unsigned int histindex=0;
  uint32_t curlen = 0;
  uint32_t i=0;
  uint32_t j;
  bool is_finished = false;
  do {
    c = ekey();
    switch(c) {
    case 1: // Ctrl-A
    case -72: // Home key  Cursor to start of line.
      moveleft(i);
      i=0;
      break;
    case 5: // Ctrl-E
    case -70: // End key Cursor to end of line
      moveright(curlen-i);
      i=curlen;
      break;
    case 4: // Ctrl-D Delete forward.
      if (curlen == 0) {
	putch('\n');
	set_irq(ENGINE_EXIT_IRQ);
	return 0;
      }
      else if (i<curlen) {
	for (j=i; j<curlen;j++) p[j]=p[j+1];
	curlen--;
	typestr(p+i, curlen-i); putch(' ');moveleft(curlen-i+1);
      }
      break;
    case 8: // Ctrl-H
    case 127: // DEL Delete backward.
      if (i>0) {
	moveleft(1);
	i--;
	for (j=i; j<curlen;j++) p[j]=p[j+1];
	curlen--;
	typestr(p+i, curlen-i); putch(' ');moveleft(curlen-i+1);	
      }
      break;      
    case 21: // Ctrl-U delete entire line.
      moveleft(i);
      i=0;
      for (j=0; j<curlen; j++) putch(' ');
      moveleft(curlen);
      curlen = 0;
      break;
    case 11: // Ctrl-K delete to end of line
      for (j=i; j<curlen; j++) putch(' ');
      moveleft(curlen-i);
      curlen = i;
      break;
    case 2:   // Ctrl-B
    case -68: // Cursor left
      if (i>0) {
	i--;
	moveleft(1);
      }
      break;
    case 6:   // Ctrl-F
    case -67: // Cursor right
      if (i<curlen) {
	i++;
	moveright(1);
      }
      break;
    case 16: // Ctrl-P
    case -65: // Cursor-Up Go back in history
      if (histindex < HIST_ENTRIES) {
	histindex++;
	moveleft(i);
	for (j=0; j<curlen; j++) putch(' ');
	moveleft(curlen);
	curlen = get_hist(p, maxlen, histindex);
	typestr(p, curlen);
	i=curlen;
      }
      break;
    case 14: // Ctrl-N
    case -66: // Cursor-Down Go forward in history
      if (histindex > 1) {
	histindex--;
	moveleft(i);
	for (j=0; j<curlen; j++) putch(' ');
	moveleft(curlen);
	curlen = get_hist(p, maxlen, histindex);
	typestr(p, curlen);
	i=curlen;
      } else if (histindex == 1) {
	histindex--;
	moveleft(i);
	i=0;
	for (j=0; j<curlen; j++) putch(' ');
	moveleft(curlen);
	curlen = 0;	
      }
      break;
    case 10:
    case 13:
      is_finished = true;
      break;
    default:
      if (curlen < maxlen && c>=' ' && c<='~') {
	for (j=curlen; j>i; j--) p[j]=p[j-1];
	p[i]=c;
	typestr(p+i,curlen+1-i);i++;curlen++;moveleft(curlen-i);
      }      
    }
  } while (!is_finished);
  moveright(curlen-i);
  putch(' ');
  put_hist(p,curlen);
  return curlen;
}

void get_xy(unsigned int *x, unsigned int *y)
{
  char numbuf[14];
  char c;
  int i;
  *x=-1;
  *y=-1;
  typestr("\e[6n",4);
  while (getch() != 0x1b)
    ;
  if (getch() != '[')
    return;
  i=0;
  while (isdigit(c=getchar())) {
    if (i<13) {
      numbuf[i++]=c;
    }
  }
  numbuf[i]=0;
  *y=atoi(numbuf)-1;
  if (c != ';') return;
  i=0;
  while (isdigit(c=getchar())) {
    if (i<13) {
      numbuf[i++]=c;
    }
  }
  numbuf[i]=0;
  *x=atoi(numbuf)-1;
  if (c != 'R') return;  
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
    *--sp=ekey();
    break;
  case 1:
    *--sp=kbhit();
    break;
  case 2: /* ACCEPT */
    {
      uint8_t *p = dict_base + sp[1];
      sp[1] = editline((char*)p,sp[0]);
      sp++;
    }
    break;
  case 3: /* EMIT */
    putch(*sp++);
    break;
  case 4: /* TYPE */
    { 
      uint32_t i;
      uint8_t *p=dict_base + sp[1];
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
  case 0xb: /* USLEEP */
    usleep(*sp++);
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
	fileids[i] = fopen((char*)name_addr,filemodes[sp[0]]);
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
      p = fgets((char*)dict_base+sp[2],sp[1],fileids[sp[0]]);
      if (ferror(fileids[sp[0]])) {
	sp[2] = 0;
	sp[1] = 0;
	sp[0] = -200;
      } else {
	uint32_t l=strlen((char*)dict_base+sp[2]);
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
    rc=remove((char*)name_addr);
    sp++;
    sp[0] = rc;
    break;
  case 0x19: /* SYSTEM */
    MAKE_ASCIIZ(dict_base+sp[1],sp[0]);
    systerm();
    rc=system((char*)name_addr);
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
    rc = chdir((char*)name_addr);
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
  case 0x1F: /* GET-XY */
    sp-=2;
    get_xy(&sp[1],&sp[0]);
    break;
  case 0xc0: /* GRAHICS-PARAMS */
#ifndef USE_TERM_BITMAP
    sp-=3;
    sp[0] = 0;
    sp[1] = 0;
    sp[2] = 0;
    break;
#else
    if (tbm_width == 0) {
      tbm_get_recommended(&tbm_width,&tbm_height,&tbm_ncolors,&tbm_mode);
    }
    if (tbm_ncolors>16) tbm_ncolors = 16;
    sp-=3;
    sp[2] = tbm_width;
    sp[1] = tbm_height;
    sp[0] = tbm_ncolors;
    break;
  case 0xc1: /* GRAPHICS-MODE */
    if (tbm_width == 0) {
      tbm_get_recommended(&tbm_width,&tbm_height,&tbm_ncolors,&tbm_mode);
    }
    if (tbm_ncolors>16) tbm_ncolors = 16;
    tbm = tbm_new_screen(tbm_width, tbm_height, 0, 0, tbm_ncolors,tbm_mode);
    break;
  case 0xc2: /* TEXT-MODE */
    if (tbm) {
      tbm_delete(tbm);
      tbm=NULL;
    }
    break;
  case 0xc3: /* REDRAW */
    systerm();
    if (tbm) tbm_redraw(tbm);
    forthterm();
    break;
  case 0xc4: /* CLG */
    if (tbm) tbm_clear(tbm);
    break;
  case 0xc5: /* SETFG-G */
    if (tbm) tbm_setfg(tbm,sp[0]);
    sp++;
    break;
  case 0xc6: /* SETPEN */
    if (tbm) tbm_setpen(tbm,sp[3],sp[2],sp[1],sp[0]);
    sp+=4;
    break;
  case 0xc7: /* PLOTDOT */
    if (tbm) tbm_plotdot(tbm,sp[1],sp[0]);
    sp+=2;
    break;
  case 0xc8: /* MOVETO */
    if (tbm) tbm_moveto(tbm,sp[1],sp[0]);
    sp+=2;
    break;
  case 0xc9: /* LINETO */
    if (tbm) tbm_lineto(tbm,sp[1],sp[0]);
    sp+=2;
    break;
  case 0xca: /* TRIANGLE */
    if (tbm) tbm_triangle(tbm,sp[3],sp[2],sp[1],sp[0]);
    sp+=4;
    break;
  case 0xcb: /* CIRCLE */
    if (tbm) tbm_circle(tbm,sp[2],sp[1],sp[0],false);
    sp+=3;
    break;
  case 0xcc: /* CIRCLE-F */
    if (tbm) tbm_circle(tbm,sp[2],sp[1],sp[0],true);
    sp+=3;
    break;
  case 0xcd: /* PLOTTEXT */
    MAKE_ASCIIZ(dict_base+sp[1],sp[0]);
    if (tbm) tbm_plottext(tbm,(char*)name_addr);
    sp+=2;
    break;
  case 0xce: /* GETDOT */
    if (tbm) sp[1] = tbm_getdot(tbm,sp[1],sp[0]);
    sp+=1;
    break;
  case 0xcf: /* GETPOS */
    sp-=2;
    {
      int x,y;
      if (tbm) tbm_getpos(tbm,&x,&y);
      sp[0]=y;
      sp[1]=x;
    }
    break;
  case 0xd0: /* PUT-BITMAP-MONO */
    if (tbm) tbm_mono_bitmap_put(tbm,dict_base+sp[4],sp[3],sp[2],sp[1],sp[0]);
    sp+=5;
    break;
  case 0xd1: /* PUT-BITMAP */
    if (tbm) tbm_bitmap_put(tbm,dict_base+sp[4],sp[3],sp[2],sp[1],sp[0], false);
    sp+=5;
    break;
  case 0xd2: /* PUT-BITMAP-TRANSP */
    if (tbm) tbm_bitmap_put(tbm,dict_base+sp[4],sp[3],sp[2],sp[1],sp[0], true);
    sp+=5;
    break;
  case 0xd3: /* GET-BITMAP */
    if (tbm) tbm_bitmap_get(tbm,dict_base+sp[4],sp[3],sp[2],sp[1],sp[0]);
    sp+=5;
    break;
#endif    
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
