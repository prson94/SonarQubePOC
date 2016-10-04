import { MonitorComponent } from './monitor.component';
import { MonitorListComponent } from './monitor-list.component';

export const MonitorRoutes = [
    {
        path: 'a/monitor',
        component: MonitorComponent,
        children: [            
            { path: '', component: MonitorListComponent }
        ]
    }
];