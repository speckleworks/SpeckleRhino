<template>
  <v-layout @mouseover='mouseOver' @mouseleave='mouseOut' align-center style='border-top: 1px dashed grey'>
    <v-flex class='xs8 text-xs-left layername pl-4'>
      <span class=''>{{ layer.name }}</span>
      <span class="caption grey--text"> Object count: {{layer.objectCount }} </span>
    </v-flex>
    <v-flex class='xs1 text-xs-center'></v-flex>
    <v-flex class='xs1 text-xs-center'></v-flex>
    <v-flex class=' text-xs-center'>
      <v-btn icon>
        <v-icon dark :style='{ color: layerColor.hex }' class='make-me-small'>fiber_manual_record</v-icon>
      </v-btn>
    </v-flex>
  </v-layout>
</template>
<script>
export default {
  name: '',
  props: {
    layer: Object,
    clientId: String
  },
  components: {},
  computed: {
    layerColor( ) {
      if ( this.layer.properties && this.layer.properties.color )
        return this.layer.properties.color
      return { hex: '#AEECFD', alpha: 1 }
    }
  },
  watch: {
    visible( value ) {
      Interop.setLayerVisibility( this.clientId, this.layer.guid, value )
    }
  },
  data( ) {
    return {
      visible: true
    }
  },
  methods: {
    mouseOver( ) {
      Interop.setLayerHover( this.clientId, this.layer.guid, true )
    },
    mouseOut( ) {
      Interop.setLayerHover( this.clientId, this.layer.guid, false )
    },
    bake( ) {
      Interop.bakeLayer( this.clientId, this.layer.guid )
    }
  },
  mounted( ) {}
}
</script>
<style lang='scss'>
.make-me-small {
  font-size: 14px !important;
}

.layername {
  max-width: 50%;
  text-overflow: ellipsis;
  /* Required for text-overflow to do anything */
  white-space: nowrap;
  overflow: hidden;
}
</style>