import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HomeIndex } from './index';

describe('HomeIndex', () => {
  let component: HomeIndex;
  let fixture: ComponentFixture<HomeIndex>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [HomeIndex],
      imports: [HttpClientTestingModule]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(HomeIndex);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the app', () => {
    expect(component).toBeTruthy();
  });

  it('should retrieve weather forecasts from the server', () => {
    const mockAssetTypes = [
      { uid: '1', name: 'Business Term', description: '', class: { name: 'Business', description: '' }, path: 'Business Term' },
      { uid: '2', name: 'Application', description: '', class: { name: 'Business', description: '' }, path: 'Application' }
    ];

    component.ngOnInit();

    const req = httpMock.expectOne('/api/v2/assets/types');
    expect(req.request.method).toEqual('GET');
    req.flush(mockAssetTypes);

    expect(component.assetTypes).toEqual(mockAssetTypes);
  });
});
