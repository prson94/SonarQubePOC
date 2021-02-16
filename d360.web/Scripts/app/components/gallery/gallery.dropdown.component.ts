import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'gallery-dropdown',
    templateUrl: './gallery.dropdown.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryDropDownComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = `<p-dropdown igDropdown
                                igSize="small"
                                [appendTo]="'body'"
                                [(ngModel)]="modelVar"
                                [options]="optionsVar"></p-dropdown>`;

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "igSize", Type: "string", Description: "Size of the input. Options are small(150px), medium(308px), large(624px) and full(100%).", Default: "full" });
        this.properties.push({ Name: "ellipsisDirection", Type: "string", Description: "Direction of ellipsis. Options are ltr(left to right) or rtl(right to left)", Default: "ltr" });
        this.properties.push({ Name: "pDropdown", Type: "", Description: "All properties exposed by Prime NG Dropdown component", Default: "" });
    }
    cars = [
        { label: 'Audi', value: 'Audi' },
        { label: 'BMW', value: 'BMW' },
        { label: 'Fiat', value: 'Fiat' },
        { label: 'Ford', value: 'Ford' },
        { label: 'Honda', value: 'Honda' },
        { label: 'Jaguar', value: 'Jaguar' },
        { label: 'Mercedes', value: 'Mercedes' },
        { label: 'Renault', value: 'Renault' },
        { label: 'VW', value: 'VW' },
        { label: 'Volvo', value: 'Volvo' }
    ];

    longNames = [
        { label: 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam pharetra egestas dolor at faucibus. Nunc porttitor eros erat, non hendrerit sem dictum quis. Morbi porttitor et sem id consectetur. Pellentesque quis mattis dolor. Pellentesque imperdiet nisl eu justo tincidunt tempor. Suspendisse eu urna et lacus vestibulum facilisis', value: 'Audi' },
        { label: 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam pharetra egestas dolor at faucibus. Nunc porttitor eros erat, non hendrerit sem dictum quis. Morbi porttitor et sem id consectetur. Pellentesque quis mattis dolor. Pellentesque imperdiet nisl eu justo tincidunt tempor. Suspendisse eu urna et lacus vestibulum facilisis', value: 'Aud2i' },
        { label: 'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam pharetra egestas dolor at faucibus. Nunc porttitor eros erat, non hendrerit sem dictum quis. Morbi porttitor et sem id consectetur. Pellentesque quis mattis dolor. Pellentesque imperdiet nisl eu justo tincidunt tempor. Suspendisse eu urna et lacus vestibulum facilisis', value: 'Aud3i' }
    ];

    longNamesPaths = [
        { label: 'Business Asset > Suma Hop test Parent_TA/Suma Hop test Child_TA/Suma Hop test Grand Child_TA/Suma Hop test Great Grand Child_TA/One More Level/Two Levels Under/Third Level/One more/Suma Hop test Child_TA/Suma Hop test Grand Child_TA/Suma Hop test Great Grand Child_TA/One More Level/Two Levels Under/Third Level/One more', value: 'Audi' },
        { label: 'Business Asset > Suma Hop test Parent_TA/Suma Hop test Child_TA/Suma Hop test Grand Child_TA/Suma Hop test Great Grand Child_TA/One More Level/Two Levels Under/Third Level/One more/Suma Hop test Child_TA/Suma Hop test Grand Child_TA/Suma Hop test Great Grand Child_TA/One More Level/Two Levels Under/Third Level/One more', value: 'Audi' },
        { label: 'Business Asset > Suma Hop test Parent_TA/Suma Hop test Child_TA/Suma Hop test Grand Child_TA/Suma Hop test Great Grand Child_TA/One More Level/Two Levels Under/Third Level/One more/Suma Hop test Child_TA/Suma Hop test Grand Child_TA/Suma Hop test Great Grand Child_TA/One More Level/Two Levels Under/Third Level/One more', value: 'Audi' },
       ];



    companies = [
        {
            "label": "Pellentesque Ltd",
            "value": "F6916C90-5695-2B6A-702A-8A1FFA8FAA1B"
        },
        {
            "label": "Dolor Nulla Semper Institute",
            "value": "A9020141-B14B-F173-B114-B5BDA7F0C98C"
        },
        {
            "label": "Morbi Accumsan Institute",
            "value": "81C648E4-62C2-0CBD-C101-CAC73C93313F"
        },
        {
            "label": "Semper Company",
            "value": "23D17919-64D1-817E-30AC-267C6514F9EA"
        },
        {
            "label": "Massa Non Ante Associates",
            "value": "9E94F16D-0454-6BB3-A7E1-1DE02149FB4E"
        },
        {
            "label": "Quisque Nonummy Ipsum LLP",
            "value": "5A5A6BEC-B26A-4D95-7A3D-D5ED104810C2"
        },
        {
            "label": "Pharetra Felis Eget Consulting",
            "value": "1EE68839-58C2-E961-DBC0-A7F822BA0DE6"
        },
        {
            "label": "Parturient Montes Consulting",
            "value": "FB01904E-B2E9-F0F2-399A-67FDC9693A08"
        },
        {
            "label": "Ridiculus Mus Foundation",
            "value": "91E4B22C-E692-61E3-4AF2-B4DCCB695869"
        },
        {
            "label": "Laoreet Libero Foundation",
            "value": "D57A1FAA-0CB2-8218-CCF7-6E86B22DD0B1"
        },
        {
            "label": "Eget LLC",
            "value": "D1B8ADE9-720B-A54B-1650-4703F3A005DA"
        },
        {
            "label": "Eros Non Ltd",
            "value": "2D42409F-5CEC-FE1F-D056-BF9B9593D05A"
        },
        {
            "label": "Etiam Ligula Tortor Industries",
            "value": "B53DF563-0FF4-0FBB-A791-4E97247A07E9"
        },
        {
            "label": "Mi Lacinia Corp.",
            "value": "759CF89A-EE9C-ABDE-7661-42052A98F39C"
        },
        {
            "label": "Vehicula Aliquet Foundation",
            "value": "A1B96214-5797-5BEC-1A81-686B9CC62F90"
        },
        {
            "label": "Mauris Corp.",
            "value": "818E1670-06D0-91AC-DB76-EF939DC0E5FD"
        },
        {
            "label": "Consectetuer Rhoncus Nullam PC",
            "value": "30A45739-22FA-67B5-64C3-8808DACEA760"
        },
        {
            "label": "Justo Proin Company",
            "value": "0CC14656-E27E-E72A-A7F1-1524C7C91446"
        },
        {
            "label": "Neque Et Limited",
            "value": "A7DF7399-4351-77A3-1E72-A433E6175EB1"
        },
        {
            "label": "Erat Vitae Risus Ltd",
            "value": "705DD63B-84AA-BDC6-0B1C-5A20BCC088E0"
        },
        {
            "label": "Semper Et Lacinia Foundation",
            "value": "0707C942-A221-4C37-FBBB-BF55259ABA58"
        },
        {
            "label": "Ante Lectus Limited",
            "value": "0396403D-0249-5763-52B8-C991DEF50358"
        },
        {
            "label": "Eleifend Nec Malesuada Inc.",
            "value": "7CE973D9-0981-E2CF-40A2-126711FE6833"
        },
        {
            "label": "Donec Limited",
            "value": "2B05E5A6-743D-7151-002F-F13A7C7014AD"
        },
        {
            "label": "Quisque LLP",
            "value": "89DE8CE2-40B0-8262-F09D-841A964231E8"
        },
        {
            "label": "Vivamus Limited",
            "value": "10F9A8B6-7488-8084-BBA8-49EA51D09ED6"
        },
        {
            "label": "Mauris A Nunc Company",
            "value": "C3B6C27E-0D0B-9BA4-9E38-D6715C042137"
        },
        {
            "label": "Lorem Tristique Aliquet LLC",
            "value": "D4E8FB44-8CBF-A592-2A0B-56A3D6524066"
        },
        {
            "label": "Sagittis Foundation",
            "value": "FA137A60-CC40-A37B-6338-62F13C0F0EDF"
        },
        {
            "label": "Ac Corporation",
            "value": "0A9A9E0A-F245-386F-EE88-11D95B3F7AD3"
        },
        {
            "label": "Lectus Corp.",
            "value": "00D0054D-D99F-C861-109A-A6865288EB6F"
        },
        {
            "label": "Hendrerit Ltd",
            "value": "87201BB3-ABBA-F682-0923-6EE03043DA5C"
        },
        {
            "label": "Iaculis Nec Eleifend LLP",
            "value": "2DC33714-A83E-67EE-6EAB-F94EA9C5D690"
        },
        {
            "label": "Fusce Mi Company",
            "value": "DA9778EA-0B5A-BDEE-73AF-38CE4FD3B0F9"
        },
        {
            "label": "Luctus Foundation",
            "value": "B9A4AE80-9959-9F53-F4B2-0F927DD942C3"
        },
        {
            "label": "Dictum Eleifend PC",
            "value": "00D625AE-4B2B-A276-E295-9D07CFEB9FBA"
        },
        {
            "label": "Sociis Natoque Consulting",
            "value": "7F28C493-D9EB-5163-1CF7-2F87AEBEA79D"
        },
        {
            "label": "Dignissim Tempor Arcu Limited",
            "value": "06DED8FB-4EDA-75D3-1A84-E5094FA8BE49"
        },
        {
            "label": "Id Associates",
            "value": "67126836-E861-5B06-B236-6CCFDA6AFABC"
        },
        {
            "label": "Arcu Et LLP",
            "value": "90C6A92E-3253-E243-4E03-A998EDC642F9"
        },
        {
            "label": "Netus Et Ltd",
            "value": "1E94E2A7-FEDC-7290-106E-F9A7504CB92C"
        },
        {
            "label": "Sit Industries",
            "value": "04DF2E80-21CD-4EAB-FE88-764B0F7E9379"
        },
        {
            "label": "Eget LLP",
            "value": "F82FED69-F9AB-889F-082B-0E29E30C54D1"
        },
        {
            "label": "Proin Vel Arcu LLP",
            "value": "99BB329A-7010-4F6B-9DDC-6A237BB7280E"
        },
        {
            "label": "Ultricies Sem Industries",
            "value": "E9EF112A-DDA8-4788-92D0-C1248080034F"
        },
        {
            "label": "Tincidunt Vehicula Risus Associates",
            "value": "8A5BAD2D-6EA1-B4BD-3ACE-3546F66E5B81"
        },
        {
            "label": "Mi Company",
            "value": "3838EC33-3090-52D3-0B61-B007BE8DA15A"
        },
        {
            "label": "Sollicitudin Incorporated",
            "value": "F0A8F593-D41A-918F-F3B7-39D81E181F2E"
        },
        {
            "label": "Odio Vel Corp.",
            "value": "C73049C3-267F-190D-19DA-1EAE990349F0"
        },
        {
            "label": "Dictum Mi Ac Industries",
            "value": "E475FBFB-6297-D33E-7A10-7862DFD3EEA4"
        },
        {
            "label": "A Feugiat Tellus Foundation",
            "value": "E477A705-73C3-BE3E-E9DB-95404376A3EA"
        },
        {
            "label": "Mauris A Nunc PC",
            "value": "9D3E8EB5-094B-5FF8-272B-A642B25A742A"
        },
        {
            "label": "Natoque Penatibus Company",
            "value": "42D4902C-2BCB-B869-239F-FC32867F6D19"
        },
        {
            "label": "Non Dapibus Rutrum Corp.",
            "value": "96C557DC-F044-50C8-4FD4-551A8EBEBD8B"
        },
        {
            "label": "Quam Elementum Consulting",
            "value": "EAD83014-D50F-7087-C29D-8900CC9F7C2D"
        },
        {
            "label": "Enim Curabitur LLC",
            "value": "D5AB478D-9708-1A62-EA05-9A875715CB74"
        },
        {
            "label": "Id Consulting",
            "value": "2C82CA6D-28C2-4FB2-D753-AA2595FEB3FC"
        },
        {
            "label": "Sem Nulla Interdum Ltd",
            "value": "11596EF8-69E0-9CE8-F653-2A8B7EFAFF1F"
        },
        {
            "label": "Fringilla Corp.",
            "value": "466D2703-F9CE-A531-66AF-3402876585E6"
        },
        {
            "label": "Cras Convallis Convallis Incorporated",
            "value": "3AB9C69B-C038-BE36-D9DC-8BF4C8C00182"
        },
        {
            "label": "Iaculis Enim Industries",
            "value": "C22148CF-9D75-ECA8-A361-DE1FC7B51646"
        },
        {
            "label": "Phasellus Nulla Integer Limited",
            "value": "01D70883-4CBA-8B88-8D1F-2AE3A1744BE4"
        },
        {
            "label": "Lorem Semper Auctor Foundation",
            "value": "726DBAEE-95D1-7B54-30FE-3B9F31E6F286"
        },
        {
            "label": "Nunc Commodo Auctor Associates",
            "value": "5208EC13-9A3A-414C-5609-B8045CDC5067"
        },
        {
            "label": "Et Consulting",
            "value": "3246247F-07A8-F290-0449-1B11F95DD3E6"
        },
        {
            "label": "Aliquet LLC",
            "value": "38D2C49A-7897-9607-7FA0-E69D37139F29"
        },
        {
            "label": "Ipsum Dolor Company",
            "value": "401DCCD3-8988-E673-ACD5-AEB27F373EC9"
        },
        {
            "label": "Aptent LLP",
            "value": "B441C6CD-9EB8-D6A3-AF06-F27FD31D9288"
        },
        {
            "label": "Erat Volutpat Nulla Institute",
            "value": "6933B8A7-B748-6F93-ECA1-C1B066DA6BD4"
        },
        {
            "label": "Diam Nunc Ullamcorper LLP",
            "value": "821280F0-8A1D-C9C4-355D-EE000268FA19"
        },
        {
            "label": "Eget Institute",
            "value": "B8BC7C9F-F53B-FC49-7BFD-10754580F957"
        },
        {
            "label": "Nam Corp.",
            "value": "8518829E-B69A-6DC9-3CD2-2039CA5AC2CF"
        },
        {
            "label": "Morbi Non Sapien Associates",
            "value": "70D23008-CEB2-A6A4-495C-3B516D0E6802"
        },
        {
            "label": "Adipiscing Institute",
            "value": "3CA6AFD2-A92D-6DE4-C0ED-A5EF1F8CA0FF"
        },
        {
            "label": "In Limited",
            "value": "6B16075B-5B66-6CB5-3783-49F505FB2669"
        },
        {
            "label": "Sed Pede Nec PC",
            "value": "5D0FD101-5B59-AD16-D38C-356748EA1B95"
        },
        {
            "label": "Nulla Dignissim Maecenas LLC",
            "value": "71F4ED05-1497-9AFA-8A6B-9E5520ED8241"
        },
        {
            "label": "Parturient Montes Nascetur Company",
            "value": "82828D3C-6262-B6FD-8C1A-05F54F03A6E3"
        },
        {
            "label": "Elit Nulla Inc.",
            "value": "37812653-03DA-03AA-69EA-B5D13AE97218"
        },
        {
            "label": "Nunc Sollicitudin Inc.",
            "value": "72A75420-E2EE-BA2E-8B81-C6E4901C6761"
        },
        {
            "label": "Nunc Sed Orci Associates",
            "value": "B2E24EB0-5C51-A46D-C2C0-1CAC04D2BEE4"
        },
        {
            "label": "Aliquam Consulting",
            "value": "12D06CDB-30BE-8752-78F0-EC46F84D9DD2"
        },
        {
            "label": "Sapien Industries",
            "value": "881A93AD-8416-3BE2-8745-B7B8D6EC8C70"
        },
        {
            "label": "Ornare Corporation",
            "value": "22BEB16F-6F52-0342-5DF8-A569CBBF05A9"
        },
        {
            "label": "Enim Associates",
            "value": "4EE8CECB-6C0E-27AC-E33E-74511E689BD2"
        },
        {
            "label": "Non Feugiat Consulting",
            "value": "F59FB6E7-4BAE-5D4B-030E-E56F8346D6F4"
        },
        {
            "label": "In Scelerisque Scelerisque Industries",
            "value": "0BC7D292-7EBE-8D19-1EC9-F00F6CF06689"
        },
        {
            "label": "At Limited",
            "value": "567776DF-3E3C-C085-31C2-8058C1EAC870"
        },
        {
            "label": "Arcu Limited",
            "value": "04E76087-DD98-09D2-4F30-CD015054C57C"
        },
        {
            "label": "Nascetur Ridiculus Mus LLP",
            "value": "6FC49D5F-8B05-AD0B-42B4-4AD8A2BEA4EA"
        },
        {
            "label": "Risus Company",
            "value": "A7E343E1-6FB2-F6C2-4C44-6E4EAAE062E1"
        },
        {
            "label": "Sollicitudin Commodo Ipsum PC",
            "value": "3B562723-7B93-C462-F814-7A2C632B2C44"
        },
        {
            "label": "Porttitor LLP",
            "value": "667605F0-64BD-1560-D67A-427C5338ABEC"
        },
        {
            "label": "Sem Associates",
            "value": "590D7AD0-723B-44A2-DF56-D12FF3672E08"
        },
        {
            "label": "Feugiat Metus Company",
            "value": "ECD64223-6E69-9EA7-36E7-148E22E55BB1"
        },
        {
            "label": "Ornare Tortor At Industries",
            "value": "1409A84B-097F-7D5C-92FB-2BBA7E252ABA"
        },
        {
            "label": "Fusce Feugiat Company",
            "value": "3951F9DC-6117-7171-C729-18F0FE5622E2"
        },
        {
            "label": "Nulla Semper Inc.",
            "value": "43CC7FC0-02C7-CCD3-FF70-C1DD8D45953D"
        },
        {
            "label": "Eu Augue Porttitor Foundation",
            "value": "771B0052-4A6D-539D-EFE3-9143E1653420"
        },
        {
            "label": "Velit Industries",
            "value": "049857B6-F7A4-6F5B-832B-D846DE0EC3B6"
        }
    ]
}
