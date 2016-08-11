import { NgModule }       from '@angular/core';
import { BrowserModule, Title  } from '@angular/platform-browser';
import { AppComponent }   from './app.component';
import { FormsModule }    from '@angular/forms';
import { routing }        from './app.routes';
import { HttpModule }     from '@angular/http';

@NgModule({
    declarations: [AppComponent],
    imports: [
        BrowserModule,
        FormsModule,
        routing,
        HttpModule,
    ],
    bootstrap: [AppComponent],
    providers: [Title],
})
export class AppModule { }







