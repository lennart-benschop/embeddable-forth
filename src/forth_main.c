/* Embeddable C-based FORTH interpreter.
   Copyright 2025 L.C. Benschop, Vught, The Netherlands.
   The program is released under the MIT license.
   There is NO WARRANTY.
*/

#include <stdint.h>
#include <stdbool.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>

#ifndef FORTH_SKIP_DEFAULT_DICT
#include "default_dict.h"
#endif
#include "forth_engine.h"


int
main(int argc, char**argv)
{
  int rc;
  struct engine_state state;
  if(argc >= 3 && strcmp(argv[1],"-d")==0) {
    rc = load_engine_from_file(&state,argv[2]);
    argc-=2;
    argv+=2;
  } else {
#ifndef FORTH_SKIP_DEFAULT_DICT
    rc = load_engine_from_mem(&state,forth_default_dict);
#else
    rc = -3;
#endif    
  }
  if (rc < 0) {
    fprintf(stderr,"Error loading dictionary\n");
    switch (rc) {
    case -4: fprintf(stderr,"Cannot open file\n"); break;      
    case -3: fprintf(stderr,"Error reading from file\n"); break;      
    case -2: fprintf(stderr,"Bad magic number \n"); break;      
    case -1: fprintf(stderr,"Cannot allocate memory\n"); break;
    }
    exit(1);
  }
  forth_io_init(argc,argv);
  rc = run_engine(&state);
  forth_io_exit();
  if (rc < 0) {
    fprintf(stderr,"Error running FORTH engine %dd\n",rc);
    exit(2);
  } 
  return 0;
}
