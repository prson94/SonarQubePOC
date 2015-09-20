/// <reference path="../scripts.js" />

(function ($) {
    // /api/comments/followed?skip={skip:int}&take={take:int} (GET)
    // /api/{type}/{id:long}/comments?skip={skip:int}&take={take:int} (GET)
    // /api/comments/followed/categories (GET)
    // /api/comments/{id:long} (GET)
    // /api/comments/{id:long} (POST child)
    // /api/comments/{type}/{id:long} (POST root)
    amplify.request.define("ByFollowed", "ajax", { url: '/api/CurrentResource/followedcomments?skip={skip}&take={take}', type: 'GET' });
    amplify.request.define("ByType", "ajax", { url: '/api/{type}/{id}/comments?skip={skip}&take={take}', type: 'GET' });
    amplify.request.define("PostRootComment", "ajax", { url: '/api/comments/{type}/{id}', type: 'POST' });
    amplify.request.define("PostChildComment", "ajax", { url: '/api/comments/{id}/comments', type: 'POST' });

    var methods = {
        init: function (options) {
            var defaults = {
                enableborder: true,
                enablecategories: false,
                enablecategoryview: false,
                followed: false,
                type: null,
                id: null,
                buttons: null,
                pageSize: 200
            };

            options = $.extend(defaults, options);           // extending default with any options that were provided

            return this.each(function () {

                var $this = $(this),
                    data = $this.data('Social');

                $this.addClass("Social");

                if (!data) {

                    $(this).data('Social', {
                        Target: $this,
                        Options: options
                    });

                    reload($this, options.type, options.id);
                }
            });
        },
        destroy: function () {
            return this.each(function () {
                var $this = $(this),
                    data = $this.data('Social');

                $this.removeData('Social');
            });
        },
        reload: function (type, id) {
            return this.each(function () {
                var $this = $(this);
                reload($this, type, id)
            });
        }
    };

    $.fn.Social = function (method) {

        // Method calling logic
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on d3s.Social');
        }

    };

    //#region Private Methods

    function reload($obj, type, id) {
        //return $obj.each(function () {
        var $this = $obj,//$(this),
                data = $this.data('Social'),
                options = data.Options;

            options.type = type;
            options.id = id;

            $this.html('');     // Clear the data from this element.

            elemID = $this[0].id;

            $this.addClass("social");

            if (options.enablecategoryview) {

                var treeID = elemID + 'Tree';

                $this.append('<div id="' + treeID + '"></div>');

                $('#' + treeID).jqxTree();

                $('#' + treeID).bind("select", function (evt) {
                    var node = $(evt.args.element);//.find(".node");
                    var id = node.data("categoryid");
                    var ca = node.data("category");
                    amplify.publish("SocialPostsFiltered", { category: ca, categoryid: id });
                });

            }
            else {

                $this.append('<h3>Add a note:</h3>');

                var txt = $('<textarea id="comment" class="post"></textarea>');
                $this.append(txt);
                if (options.buttons) {
                    txt.redactor({ focus: true, buttons: options.buttons, autoresize: true });
                }
                else {
                    txt.redactor({ focus: true, autoresize: false });
                }

                var toolbar = $('<div class="toolbar"></div>');

                var addcomment = $('<button><i class="fa fa-comment">Add comment</i></button>');
                addcomment.on('click', function () {
                    var txt = $('#comment');
                    //txt.getCode()
                    amplify.request("PostRootComment", { type: type, id: id, Comment: txt.redactor('get') }, function (data) {
                        loadLevel($this, data);
                        txt.redactor('set','');
                    });
                    txt.redactor('set', '');
                });
                toolbar.append(addcomment);

                $this.append(toolbar);

                getMorePosts($this, type, id);

                amplify.subscribe("SocialPostsFiltered", function (data) {
                    showOnlyRelevantCategory($this, data.category, data.categoryid);
                });

                $(window).scroll(function () {
                    if ($(window).scrollTop() == $(document).height() - $(window).height()) {
                        getMorePosts($this);
                    }
                });

            }

        //});
    }

    function getMorePosts($obj) {

        var data = $obj.data('Social'),
            options = data.Options;

        var count = $obj.children('.entry').size();

        if (options.followed) {
            amplify.request("ByFollowed", { skip: count, take: 50 }, function (data) {
                loadPosts($obj, data);
            });
        }
        else {
            if (options.type && options.type)
            {
                amplify.request("ByType", { type: options.type, id: options.id, skip: count, take: 50 }, function (data) {
                    loadPosts($obj, data);
                });
            }
        }
    }
    
    function loadPosts($obj, data) {

        loadLevel($obj, data);

        // Make sure to only match links data-type tag
        //$('a[data-type]').each(function () {
        //    addTooltip(this);
        //});
    }

    function loadLevel($obj, data, parentID, options) {

        if ($obj.data('Social'))
        {
            options = $obj.data('Social').Options;
        }

        $.each(data, function (x, c) {

            if (c.ParentID == parentID) {
                var entry = $('<div class="entry" id="' + c.ID + '" data-objectid="' + c.ObjectID + '" data-type="' + c.ObjectType + '"></div>');

                entry.append('<div class="profile"><img src="/resources/image/' + c.CreatingResourceID + '?size=30" style="border-radius: 3px"/></div>');//entry.append('<div class="profile"><img src="https://secure.gravatar.com/avatar/' + hex_md5 (c.ResourceEmail) + '?s=30" style="border-radius: 3px"/></div>');
                entry.append('<div class="name"><a data-type="' + c.ObjectType + '" data-context="Preview" data-id="' + c.ObjectID + '" href="' + c.ObjectUrl + '">' + c.ObjectName + '</a></div>');

                var authorHtml = "";
                authorHtml += '<div class="date">on ' + formatDate(c.DateCreated);
                if (c.ObjectType != 'Resource' && c.ObjectID != c.CreatingResourceID) {
                    authorHtml += '<br/>Posted by <a data-type="Resource" data-context="Preview" data-id="' + c.CreatingResourceID + '" href="/resources/' + c.CreatingResourceID + '">' + c.ResourceName + '</a>';
                }
                authorHtml += '</div>';

                entry.append(authorHtml);

                entry.append('<div class="message">' + c.Body + '</div>');

                var comments = $('<div class="comments"></div>');

                var toolbar = $('<div class="toolbar"></div>');
                var reply = $('<button><i class="fa fa-comment-alt">Reply</i></button>');
                reply.data('parentid', c.ID);

                reply.on("click", function () {
                    var txt = $('<textarea id="comment' + c.ID + '" class="htmlArea"></textarea>');
                    toolbar.append(txt);
                    if (options) {
                        txt.redactor({ height: 250, width: 500, buttons: options.buttons });
                    }
                    else
                    {
                        txt.redactor({ height: 250, width: 500, buttons: [] });
                    }
                    reply.off("click");
                    var addcomment = $('<button><i class="fa fa-comment">Add comment</i></button>');
                    addcomment.data('parentid', c.ID);

                    addcomment.one('click', function () {
                        var parentID = $(this).data('parentid');
                        var txt = $('#comment' + parentID);

                        amplify.request("PostChildComment", { id: parentID, Comment: txt.redactor('get') }, function (data) {
                            loadLevel(comments, data, parentID, options);
                            txt.redactor('set', '');
                        });

                        txt.redactor('destroy');
                        txt.fadeOut(500);
                        $(this).fadeOut(500);
                    });
                    toolbar.append(addcomment);
                });
                toolbar.append(reply);
                entry.append(toolbar);

                loadLevel(comments, data, c.ID);
                entry.append(comments);

                $obj.append(entry);
            }
        });
    }
    
    function showOnlyRelevantCategory($obj, category, id) {

        var checkID = (id != '0');

        $obj.children('.entry').each(function () {

            if (category == '') {
                $(this).show(300);
            }
            else {
                if ($(this).data("type") == category) {
                    if (checkID) {
                        if ($(this).data("objectid") == id) {
                            $(this).show(300);
                        }
                        else {
                            $(this).hide(300);
                        }
                    }
                    else {
                        $(this).show(300);
                    }
                }
                else {
                    $(this).hide(300);
                }
            }
        });
    }

    //#endregion

})(jQuery);