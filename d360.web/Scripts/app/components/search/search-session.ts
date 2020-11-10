import { SearchState } from '../../models/search-result.model';

export class SearchSession {
    private static readonly sessionKey: string = 'd360SearchState';
    private static readonly sessionAgeMinutes: number = 10;

    static removeState(term: string) {
        let sess: SearchState[] = JSON.parse(sessionStorage.getItem(this.sessionKey));
        if (sess == null) {
            sess = [];
        } else {
            let limit = new Date().getTime() - (this.sessionAgeMinutes * 60000)
            sess = sess.filter(q => q.Term != term && new Date(q.Querytime).getTime() > limit);
        }
        sessionStorage.setItem(this.sessionKey, JSON.stringify(sess));
    }

    static getState(term: string): SearchState {
        let state: SearchState = undefined;
        let sess: SearchState[] = JSON.parse(sessionStorage.getItem(this.sessionKey));
        let limit = new Date().getTime() - (this.sessionAgeMinutes * 60000)
        if (sess != null && sess.findIndex(q => q.Term == term && new Date(q.Querytime).getTime() > limit) >= 0) {
            state = sess.find(q => q.Term == term);
        }
        return state;
    }

    static putState(state: SearchState) {
        let sess: SearchState[] = JSON.parse(sessionStorage.getItem(this.sessionKey));
        if (sess == null) {
            sess = [];
        } else {
            let limit = new Date().getTime() - (this.sessionAgeMinutes * 60000)
            sess = sess.filter(q => q.Term != state.Term && new Date(q.Querytime).getTime() > limit);
        }
        sess.push(state);
        sessionStorage.setItem(this.sessionKey, JSON.stringify(sess));
    }
}